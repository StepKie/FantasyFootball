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
	CompetitionType _selectedCompetitionType;

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
		SelectedCompetitionType = _dataService.SelectedCompetitionType;
		SelectedYear = _dataService.SelectedCompetitionYear;
		ResetToHistoricTeams();
	}

	public IList<CompetitionType> CompetitionTypes { get; } = new[] { CompetitionType.WM, CompetitionType.EM };

	public IList<int> Years => SelectedCompetitionType.AvailableYears().ToList();
	public ImageSource CompetitionLogo => IconStrings.GetCompetitionLogo(SelectedCompetitionType);
	public TeamViewModel? SelectedTeam { get; set; }
	public List<TeamsGroup> TeamsByGroup => new(Groups.Select(group => new TeamsGroup(group)));

	[ICommand]
	void ResetToHistoricTeams() => Groups = GroupFactory.For(_dataService, SelectedCompetitionType).CreateFromHistoricalData(SelectedYear);

	[ICommand]
	void FillRandomTeams() => Groups = GroupFactory.For(_dataService, SelectedCompetitionType).DrawRandom();

	[ICommand]
	async Task SimulateSingle()
	{
		AppShell.SetGamesVisible(true);
		var competition = await CreateCompetition();
		ServiceHelper.GetService<StandingsViewModel>()!.UpdateStandings(competition);
		var route = $"//PlayTab/{nameof(GamesPage)}?{nameof(GamesViewModel.CompetitionId)}={competition.Id}";
		await Shell.Current.GoToAsync(route);
	}

	[ICommand]
	async Task SimulateBatch()
	{
		await Shell.Current.GoToAsync("..").ConfigureAwait(false);
		// TODO Show Progress on CompetitionsPage
		for (int i = 1; i <= DefaultAmountOfBatchSimulations; i++)
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
		_dataService.SelectedCompetitionType = value;
		SelectedYear = value.AvailableYears().Last();
	}

	partial void OnSelectedYearChanged(int value)
	{
		_dataService.SelectedCompetitionYear = value;
		ResetToHistoricTeams();
	}

	partial void OnNewTeamIdSelectedChanged(int value)
	{
		if (SelectedTeam is not null)
		{
			Group containingGroup = Groups.First(g => g.Teams.Contains(SelectedTeam.Team));
			containingGroup.Teams.Replace(t => t.Equals(SelectedTeam.Team), Repo.Get<Team>(value)!);
			OnPropertyChanged(nameof(Groups));
			// AlsoNotifyChangeFor only notifies via setter, so we need to notify manually
			OnPropertyChanged(nameof(TeamsByGroup));
			SelectedTeam = null;
		}
	}

	async Task<Competition> CreateCompetition()
	{
		IsBusy = true;
		var factory = CompetitionFactory.For(SelectedCompetitionType, SelectedYear, Groups);
		var competition = factory.Create();
		Repo.Save(competition);
		Log.Debug("Competition created");
		IsBusy = false;

		return competition;
	}
}
