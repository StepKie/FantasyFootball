using CommunityToolkit.Mvvm.ComponentModel;
using FantasyFootball.Data;
using FantasyFootball.Data.CompetitionFactories;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;

namespace FantasyFootball.UI.ViewModels;

/// <summary>
/// Backs the /competitions/setup page. Pick a competition type + year,
/// reset the participants to the historical lineup or draw a random one
/// weighted by Elo, then simulate. The page navigates to /competitions/{id}
/// on simulate so the user lands in the games + standings view.
///
/// MAUI's <c>CompetitionSetupViewModel</c> also supports per-team manual
/// edit (round-trip to TeamsPage with SelectionType.RETURN_ID) and batch
/// simulation; both are deferred for the web port — manual edit will land
/// as a MudDialog when needed, batch sim is a small follow-up.
///
/// Per-run Elo override (the "make my favourite team stronger for this
/// run only" feature from the user story) is deferred behind #19.
/// </summary>
public partial class CompetitionSetupViewModel : ObservableObject
{
	readonly IRepository _repo;
	readonly IDataService _dataService;
	// Suppresses the OnSelectedYearChanged → ResetToHistoricTeams cascade during ctor,
	// so we don't pay for CSV parsing twice on page load (once via the type-change
	// cascade, once via the explicit SelectedYear assignment below).
	readonly bool _initialized;

	public CompetitionSetupViewModel(IRepository repo, IDataService dataService)
	{
		_repo = repo;
		_dataService = dataService;

		SelectedCompetitionType = _dataService.SelectedCompetitionType;
		// Guard: if the persisted year isn't available for the current type,
		// fall back to the most recent year for that type.
		var validYears = SelectedCompetitionType.AvailableYears().ToList();
		SelectedYear = validYears.Contains(_dataService.SelectedCompetitionYear)
			? _dataService.SelectedCompetitionYear
			: validYears.Last();

		_initialized = true;
		ResetToHistoricTeams();
	}

	public IList<CompetitionType> CompetitionTypes { get; } = [CompetitionType.WM, CompetitionType.EM];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Years))]
	public partial CompetitionType SelectedCompetitionType { get; set; }

	[ObservableProperty]
	public partial int SelectedYear { get; set; }

	[ObservableProperty]
	public partial List<Group> Groups { get; set; } = [];

	[ObservableProperty]
	public partial bool IsBusy { get; set; }

	public IList<int> Years => SelectedCompetitionType.AvailableYears().ToList();

	public void ResetToHistoricTeams() =>
		Groups = GroupFactory.For(_dataService, SelectedCompetitionType, SelectedYear)
			.CreateFromHistoricalData(SelectedYear);

	public void FillRandomTeams() =>
		Groups = GroupFactory.For(_dataService, SelectedCompetitionType, SelectedYear).DrawRandom();

	public Competition Create()
	{
		IsBusy = true;
		try
		{
			var factory = CompetitionFactory.For(SelectedCompetitionType, SelectedYear, Groups);
			var competition = factory.Create();
			_repo.Save(competition);

			return competition;
		}
		finally
		{
			IsBusy = false;
		}
	}

	partial void OnSelectedCompetitionTypeChanged(CompetitionType value)
	{
		_dataService.SelectedCompetitionType = value;
		SelectedYear = value.AvailableYears().Last();
	}

	partial void OnSelectedYearChanged(int value)
	{
		_dataService.SelectedCompetitionYear = value;
		if (_initialized) { ResetToHistoricTeams(); }
	}
}
