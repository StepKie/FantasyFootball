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

	/// <summary>
	/// Active / Finished tab selection. Auto-defaults on navigation:
	/// 0 (Active) when at least one active competition of the selected type
	/// exists, otherwise 1 (Finished) — so navigating back from a finished
	/// competition lands on the tab that actually has content.
	/// </summary>
	[ObservableProperty]
	public partial int ActiveTabIndex { get; set; }

	/// <summary>
	/// Refresh the type filter from <see cref="IDataService"/> and auto-select
	/// the tab based on what's currently available. The VM is registered
	/// <c>AddScoped</c>, so without this call the filter stays on whatever was
	/// picked at first navigation — even if the user has since viewed a
	/// competition of a different type elsewhere.
	/// </summary>
	public void SyncFromDataService()
	{
		var freshType = _dataService.SelectedCompetitionType;
		if (SelectedCompetitionType != freshType)
		{
			// Setter triggers OnSelectedCompetitionTypeChanged → Reload, which refreshes
			// ActiveCompetitions / FinishedCompetitions for the new type.
			SelectedCompetitionType = freshType;
		}
		ActiveTabIndex = ActiveCompetitions.Count > 0 ? 0 : 1;
	}

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
			// Reuse FinishedCompetitions so the "finished" predicate stays in one place.
			OverallRecords = Standings.CreateFrom(FinishedCompetitions.SelectMany(c => c.GamesByDate));
		}
		finally
		{
			IsBusy = false;
		}
	}
}
