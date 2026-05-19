using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FantasyFootball.Data;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using static FantasyFootball.Messaging;

namespace FantasyFootball.UI.ViewModels;

/// <summary>
/// Backs the merged /competitions page (Active + Finished tabs, plus the
/// all-time TeamRecord aggregate that used to live on /statistics). Owns the
/// CompetitionType filter; row navigation and the Start-new button are
/// handled by the .razor page.
///
/// MAUI splits this into CompetitionsViewModel + StatisticsViewModel; the
/// web port collapses them since both halves are filtered by the same
/// CompetitionType and render on one screen.
/// </summary>
public partial class CompetitionsViewModel : ObservableObject
{
	readonly IRepository _repo;
	readonly IDataService _dataService;

	public CompetitionsViewModel(IRepository repo, IDataService dataService)
	{
		_repo = repo;
		_dataService = dataService;

		MessageBus.Register<CompetitionCreatedMessage>(this, (_, _) => Reload());
		MessageBus.Register<CompetitionFinishedMessage>(this, (_, _) => Reload());
		MessageBus.Register<CompetitionDeletedMessage>(this, (_, _) => Reload());
		MessageBus.Register<DataResetMessage>(this, (_, _) => Reload());

		SelectedCompetitionType = dataService.SelectedCompetitionType;
		Reload();
	}

	// Only WM + EM ship implementations today — CHAMPIONS_LEAGUE and DOMESTIC_LEAGUE
	// throw NotImplementedException in HistoricalData.AvailableYears (roadmap #20).
	public IList<CompetitionType> CompetitionTypes { get; } = [CompetitionType.WM, CompetitionType.EM];

	[ObservableProperty]
	public partial CompetitionType SelectedCompetitionType { get; set; }

	[ObservableProperty]
	public partial ObservableCollection<Competition> ActiveCompetitions { get; set; } = [];

	[ObservableProperty]
	public partial ObservableCollection<Competition> FinishedCompetitions { get; set; } = [];

	[ObservableProperty]
	public partial IList<TeamRecord> OverallRecords { get; set; } = [];

	[ObservableProperty]
	public partial bool IsBusy { get; set; }

	partial void OnSelectedCompetitionTypeChanged(CompetitionType value)
	{
		_dataService.SelectedCompetitionType = value;
		Reload();
	}

	public void Reload()
	{
		IsBusy = true;
		try
		{
			var ofType = _repo.GetAll<Competition>()
				.Where(c => c.Type == SelectedCompetitionType)
				.OrderByDescending(c => c.SimulationStart)
				.ToList();

			ActiveCompetitions = new ObservableCollection<Competition>(ofType.Where(c => !c.IsFinished));
			FinishedCompetitions = new ObservableCollection<Competition>(ofType.Where(c => c.IsFinished));
			OverallRecords = Standings.CreateFrom(ofType.Where(c => c.IsFinished).SelectMany(c => c.GamesByDate));
		}
		finally
		{
			IsBusy = false;
		}
	}
}
