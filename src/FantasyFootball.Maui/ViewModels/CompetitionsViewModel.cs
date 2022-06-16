namespace FantasyFootball.ViewModels;

[QueryProperty(nameof(SelectedCompetitionType), nameof(SelectedCompetitionType))]
public partial class CompetitionsViewModel : GeneralViewModel
{
	public List<Competition> StoredCompetitionsForSelectedType { get; private set; } = new();

	// TODO Support remaining types
	public IList<CompetitionType> CompetitionTypes { get; } = new[] { CompetitionType.WM, CompetitionType.EM };

	public IList<int> Years => SelectedCompetitionType.AvailableYears().ToList();
	public IList<TeamSelectionType> ParticipantSelectionTypes { get; } = Enum.GetValues(typeof(TeamSelectionType)).Cast<TeamSelectionType>().ToList();

	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(StoredCompetitionsForSelectedType))]
	[AlsoNotifyChangeFor(nameof(CompetitionLogo))]
	[AlsoNotifyChangeFor(nameof(Years))]
	[AlsoNotifyChangeFor(nameof(SelectedYear))]
	CompetitionType _selectedCompetitionType = CompetitionType.EM;

	[ObservableProperty]
	TeamSelectionType _selectedParticipantMode = TeamSelectionType.HISTORIC;

	[ObservableProperty]
	int _selectedYear;

	[ObservableProperty]
	int _defaultAmountOfBatchSimulations = 5;

	public ImageSource CompetitionLogo => IconStrings.GetCompetitionLogo(SelectedCompetitionType);

	[ICommand]
	async Task OpenCompetition(int competitionId)
	{
		var route = $"//PlayTab/{nameof(GamesPage)}?{nameof(GamesViewModel.CompetitionId)}={competitionId}";
		await Shell.Current.GoToAsync(route);
	}

	[Time]
	[ICommand]
	async Task OpenNewCompetition()
	{
		IsBusy = true;
		var competition = await CompetitionFactories.Create(Repo, SelectedCompetitionType, SelectedParticipantMode);
		Repo.Save(competition);
		Log.Debug("Competition created");
		ServiceHelper.GetService<StandingsViewModel>()!.LoadCompetition(competition);
		IsBusy = false;
		await OpenCompetition(competition.Id);
	}

	partial void OnSelectedCompetitionTypeChanged(CompetitionType value)
	{
		_ = ReloadCompetitions();
	}

	/// <summary> Enable reloading from OnNavigatedTo (when db is reset from another page) </summary>
	public async Task ReloadCompetitions()
	{
		IsBusy = true;
		var results = await Repo.GetAllAsync<Competition>().ConfigureAwait(false);
		StoredCompetitionsForSelectedType = new(results.Where(c => c.Type == SelectedCompetitionType));
		IsBusy = false;
		OnPropertyChanged(nameof(StoredCompetitionsForSelectedType));

	}

	[ICommand]
	async Task BatchSimulate()
	{
		for (int i = 0; i < DefaultAmountOfBatchSimulations; i++)
		{
			IsBusy = true;
			var competition = await CompetitionFactories.Create(Repo, SelectedCompetitionType, SelectedParticipantMode);
			Repo.Save(competition);
			var simulator = new CompetitionSimulator(competition, Repo, msGameDelay: 0);
			await simulator.Simulate().ConfigureAwait(false);
			IsBusy = false;
			await ReloadCompetitions().ConfigureAwait(false);
			Log.Debug($"Simulation {i} of {DefaultAmountOfBatchSimulations} complete.");
		}
	}
}
