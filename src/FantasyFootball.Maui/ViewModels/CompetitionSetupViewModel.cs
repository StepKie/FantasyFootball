namespace FantasyFootball.ViewModels;

[QueryProperty(nameof(SelectedCompetitionType), nameof(SelectedCompetitionType))]
[QueryProperty(nameof(NewTeamIdSelected), nameof(NewTeamIdSelected))]
public partial class CompetitionSetupViewModel : GeneralViewModel
{
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
	[AlsoNotifyChangeFor(nameof(Groups))]
	[AlsoNotifyChangeFor(nameof(TeamsByGroup))]
	CompetitionFactory _factory;

	public CompetitionSetupViewModel(IDataService dataService, IRepository repo)
	{
		Factory = dataService.CompetitionFactory;
		Factory.Participants = repo.GetAll<Team>();
	}

	public IList<CompetitionType> CompetitionTypes { get; } = new[] { CompetitionType.WM, CompetitionType.EM };

	public IList<int> Years => SelectedCompetitionType.AvailableYears().ToList();
	public ImageSource CompetitionLogo => IconStrings.GetCompetitionLogo(SelectedCompetitionType);
	public TeamViewModel? SelectedTeam { get; set; }
	public List<Group> Groups => _factory.CreateGroups();
	public List<TeamsGroup> TeamsByGroup => new(Groups.Select(group => new TeamsGroup(group)));

	[ICommand]
	void ResetToHistoricTeams()
	{
		Factory = CompetitionFactories.For(Repo, SelectedCompetitionType, TeamSelectionType.HISTORIC);
	}

	[ICommand]
	async Task FillRandomTeams()
	{
		Factory = CompetitionFactories.For(Repo, SelectedCompetitionType, TeamSelectionType.WITH_DRAWING);
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
		_factory.Groups = TeamsByGroup.Select(tg => tg.Group).ToList();
		var competition = await _factory.Create();
		Repo.Save(competition);
		Log.Debug("Competition created");
		IsBusy = false;

		return competition;
	}
}
