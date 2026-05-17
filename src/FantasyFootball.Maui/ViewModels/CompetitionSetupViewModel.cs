namespace FantasyFootball.ViewModels;

[QueryProperty(nameof(SelectedCompetitionType), nameof(SelectedCompetitionType))]
[QueryProperty(nameof(NewTeamIdSelected), nameof(NewTeamIdSelected))]
public partial class CompetitionSetupViewModel : GeneralViewModel
{
	readonly IDataService _dataService;

	[ObservableProperty]
	public partial int NewTeamIdSelected { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CompetitionLogo))]
	[NotifyPropertyChangedFor(nameof(Years))]
	[NotifyPropertyChangedFor(nameof(SelectedYear))]
	public partial CompetitionType SelectedCompetitionType { get; set; }

	[ObservableProperty]
	public partial int SelectedYear { get; set; }

	[ObservableProperty]
	public partial int DefaultAmountOfBatchSimulations { get; set; } = 5;

	[ObservableProperty]
	public partial List<Group> Groups { get; set; }

	/// <summary>
	/// Stable collection backing the Setup page's grouped CollectionView.
	/// Windows MAUI's CollectionView with IsGrouped=True crashes (stowed exception in
	/// Microsoft.UI.Xaml.dll) when ItemsSource is replaced with a fresh List reference.
	/// We keep one ObservableCollection and rebuild its contents whenever Groups changes.
	/// </summary>
	public ObservableCollection<TeamsGroup> TeamsByGroup { get; } = [];

	void RebuildTeamsByGroup()
	{
		TeamsByGroup.Clear();
		if (Groups is null) return;
		foreach (var g in Groups)
		{
			TeamsByGroup.Add(new TeamsGroup(g));
		}
	}

	partial void OnGroupsChanged(List<Group> value) => RebuildTeamsByGroup();

	public CompetitionSetupViewModel(IDataService dataService)
	{
		_dataService = dataService;
		SelectedCompetitionType = _dataService.SelectedCompetitionType;
		SelectedYear = _dataService.SelectedCompetitionYear;
		ResetToHistoricTeams();
	}

	public IList<CompetitionType> CompetitionTypes { get; } = [CompetitionType.WM, CompetitionType.EM];

	public IList<int> Years => SelectedCompetitionType.AvailableYears().ToList();
	public ImageSource CompetitionLogo => IconStrings.GetCompetitionLogo(SelectedCompetitionType);
	public TeamViewModel? SelectedTeam { get; set; }

	[RelayCommand]
	void ResetToHistoricTeams() => Groups = GroupFactory.For(_dataService, SelectedCompetitionType, SelectedYear).CreateFromHistoricalData(SelectedYear);

	[RelayCommand]
	void FillRandomTeams() => Groups = GroupFactory.For(_dataService, SelectedCompetitionType, SelectedYear).DrawRandom();

	[RelayCommand]
	async Task SimulateSingle()
	{
		AppShell.SetGamesVisible(true);
		var competition = CreateCompetition();
		ServiceHelper.GetService<StandingsViewModel>()!.UpdateStandings(competition);
		var route = $"//PlayTab/{nameof(GamesPage)}?{nameof(GamesViewModel.CompetitionId)}={competition.Id}";
		await Shell.Current.GoToAsync(route);
	}

	[RelayCommand]
	async Task SimulateBatch()
	{
		await Shell.Current.GoToAsync("..").ConfigureAwait(false);
		// TODO Show Progress on CompetitionsPage
		for (int i = 1; i <= DefaultAmountOfBatchSimulations; i++)
		{
			var competition = CreateCompetition();
			var simulator = new CompetitionSimulator(competition, Repo, msGameDelay: 0);
			IsBusy = true;
			await simulator.Simulate().ConfigureAwait(false);
			IsBusy = false;
			Log.Debug($"Simulation {i} of {DefaultAmountOfBatchSimulations} complete.");
		}
	}

	[RelayCommand]
	async Task SelectTeam(TeamViewModel old)
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
			RebuildTeamsByGroup();
			SelectedTeam = null;
		}
	}

	Competition CreateCompetition()
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
