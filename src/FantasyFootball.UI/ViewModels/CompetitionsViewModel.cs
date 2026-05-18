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
/// Backs the /competitions list page. Shows past competitions filtered by
/// CompetitionType. Row click navigation (to /competitions/{id}) and the
/// Start-new button (to /competitions/setup) are handled by the .razor page;
/// this VM only owns the type filter + the filtered roster.
///
/// MAUI's FantasyFootball.ViewModels.CompetitionsViewModel doubles as the
/// navigation coordinator (calls Shell.Current.GoToAsync on row select).
/// The web port pushes that into the page — the VM stays pure state.
/// </summary>
public partial class CompetitionsViewModel : ObservableObject
{
	readonly IRepository _repo;
	readonly IDataService _dataService;

	public CompetitionsViewModel(IRepository repo, IDataService dataService)
	{
		_repo = repo;
		_dataService = dataService;

		// Reload list when a simulation finishes (broadcast from CompetitionSimulator),
		// so navigating back to /competitions after a sim shows fresh data.
		MessageBus.Register<CompetitionFinishedMessage>(this, (_, _) => ReloadCompetitions());

		SelectedCompetitionType = dataService.SelectedCompetitionType;
		ReloadCompetitions();
	}

	// Only WM + EM ship implementations today — CHAMPIONS_LEAGUE and DOMESTIC_LEAGUE
	// throw NotImplementedException in HistoricalData.AvailableYears (roadmap #20).
	public IList<CompetitionType> CompetitionTypes { get; } = [CompetitionType.WM, CompetitionType.EM];

	[ObservableProperty]
	public partial CompetitionType SelectedCompetitionType { get; set; }

	[ObservableProperty]
	public partial ObservableCollection<Competition> StoredCompetitionsForSelectedType { get; set; } = [];

	[ObservableProperty]
	public partial bool IsBusy { get; set; }

	partial void OnSelectedCompetitionTypeChanged(CompetitionType value)
	{
		_dataService.SelectedCompetitionType = value;
		ReloadCompetitions();
	}

	public void ReloadCompetitions()
	{
		IsBusy = true;
		try
		{
			var filtered = _repo.GetAll<Competition>()
				.Where(c => c.Type == SelectedCompetitionType)
				.OrderByDescending(c => c.SimulationStart);
			StoredCompetitionsForSelectedType = new ObservableCollection<Competition>(filtered);
		}
		finally
		{
			IsBusy = false;
		}
	}
}
