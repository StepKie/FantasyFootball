namespace FantasyFootball.ViewModels;

[QueryProperty(nameof(SelectedCompetitionType), nameof(SelectedCompetitionType))]
[QueryProperty(nameof(NewTeamIdSelected), nameof(NewTeamIdSelected))]
public partial class CompetitionSetupViewModel : GeneralViewModel
{
	IDataService _dataService;
	[ObservableProperty]
	int _newTeamIdSelected;

	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(CompetitionLogo))]
	[AlsoNotifyChangeFor(nameof(Years))]
	[AlsoNotifyChangeFor(nameof(SelectedYear))]
	CompetitionType _selectedCompetitionType = CompetitionType.EM;

	[ObservableProperty]
	int _selectedYear;

	[ObservableProperty]
	int _defaultAmountOfBatchSimulations = 5;

	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(TeamsByGroup))]
	List<Group> _groups;

	public CompetitionSetupViewModel(IDataService dataService)
	{
		_dataService = dataService;
		ResetToHistoricTeams();
	}

	public IList<CompetitionType> CompetitionTypes { get; } = new[] { CompetitionType.WM, CompetitionType.EM };

	public IList<int> Years => SelectedCompetitionType.AvailableYears().ToList();
	public ImageSource CompetitionLogo => IconStrings.GetCompetitionLogo(SelectedCompetitionType);
	public TeamViewModel? SelectedTeam { get; set; }
	public List<TeamsGroup> TeamsByGroup => new(Groups.Select(group => new TeamsGroup(group)));

	[ICommand]
	void ResetToHistoricTeams()
	{
		Groups = GroupFactory.For(_dataService, SelectedCompetitionType).CreateFromHistoricalData();
	}

	[ICommand]
	async Task FillRandomTeams()
	{
		Groups = GroupFactory.For(_dataService, SelectedCompetitionType).DrawRandom();
	}

	[ICommand]
	async Task SimulateSingle()
	{
		var competition = await CreateCompetition();
		ServiceHelper.GetService<StandingsViewModel>()!.UpdateStandings(competition);
		var route = $"//PlayTab/{nameof(GamesPage)}?{nameof(GamesViewModel.CompetitionId)}={competition.Id}";
		await Shell.Current.GoToAsync(route);
	}

	[ICommand]
	async Task SimulateBatch()
	{
		await Shell.Current.GoToAsync("..");
		// TODO Show Progress on CompetitionsPage
		for (int i = 0; i < DefaultAmountOfBatchSimulations; i++)
		{
			var competition = await CreateCompetition();
			var simulator = new CompetitionSimulator(competition, Repo, msGameDelay: 0);
			IsBusy = true;
			await simulator.Simulate().ConfigureAwait(false);
			IsBusy = false;
			Log.Debug($"Simulation {i} of {DefaultAmountOfBatchSimulations} complete.");
		}
	}

	[ICommand]
	async void SelectTeam(TeamViewModel old)
	{
		SelectedTeam = old;
		await Shell.Current.GoToAsync($"{nameof(TeamsPage)}?{nameof(TeamsViewModel.SelectionMode)}={(int)SelectionType.RETURN_ID}");
	}

	partial void OnSelectedCompetitionTypeChanged(CompetitionType value)
	{
		ResetToHistoricTeams();
	}

	partial void OnNewTeamIdSelectedChanged(int value)
	{
		if (SelectedTeam is not null)
		{
			// Update TeamViewModel in TeamsByGroup, then clear the selection
			SelectedTeam.Team = Repo.Get<Team>(value)!;
			SelectedTeam = null;
		}
	}

	async Task<Competition> CreateCompetition()
	{
		IsBusy = true;
		var groups = TeamsByGroup.Select(tg => tg.Group).ToList();
		var factory = CompetitionFactory.For(SelectedCompetitionType, Groups);
		var competition = factory.Create();
		Repo.Save(competition);
		Log.Debug("Competition created");
		IsBusy = false;

		return competition;
	}
}
