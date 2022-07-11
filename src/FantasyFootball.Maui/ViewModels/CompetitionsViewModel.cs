namespace FantasyFootball.ViewModels;

[QueryProperty(nameof(SelectedCompetitionType), nameof(SelectedCompetitionType))]
public partial class CompetitionsViewModel : GeneralViewModel
{
	public List<Competition> StoredCompetitionsForSelectedType { get; private set; } = new();

	// TODO Support remaining types
	public IList<CompetitionType> CompetitionTypes { get; } = new[] { CompetitionType.WM, CompetitionType.EM };

	public IList<int> Years => SelectedCompetitionType.AvailableYears().ToList();

	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(StoredCompetitionsForSelectedType))]
	[AlsoNotifyChangeFor(nameof(CompetitionLogo))]
	[AlsoNotifyChangeFor(nameof(Years))]
	[AlsoNotifyChangeFor(nameof(SelectedYear))]
	CompetitionType _selectedCompetitionType = CompetitionType.EM;

	[ObservableProperty]
	int _selectedYear;

	[ObservableProperty]
	Competition? _selectedCompetition;

	[ObservableProperty]
	int _defaultAmountOfBatchSimulations = 5;

	public CompetitionsViewModel()
	{
		MessagingCenter.Subscribe<CompetitionSimulator>(this, MessageKeys.CompetitionFinished, async _ => await ReloadCompetitions());
		_ = ReloadCompetitions();
	}

	public ImageSource CompetitionLogo => IconStrings.GetCompetitionLogo(SelectedCompetitionType);

	[ICommand]
	async Task OpenCompetition(Competition competition)
	{
		ServiceHelper.GetService<StandingsViewModel>()!.UpdateStandings(competition);
		var route = $"//PlayTab/{nameof(GamesPage)}?{nameof(GamesViewModel.CompetitionId)}={competition.Id}";
		await Shell.Current.GoToAsync(route);
	}

	[ICommand]
	async Task SetupNewCompetition()
	{
		AppShell.SetGamesVisible(true);
		await Shell.Current.GoToAsync($"{nameof(CompetitionSetupPage)}");
	}

	async partial void OnSelectedCompetitionTypeChanged(CompetitionType value)
	{
		await ReloadCompetitions();
	}

	/// <summary> Enable reloading from OnNavigatedTo (when db is reset from another page) </summary>
	[ICommand]
	public async Task ReloadCompetitions()
	{
		IsBusy = true;
		var results = await Repo.GetAllAsync<Competition>().ConfigureAwait(false);
		StoredCompetitionsForSelectedType = new(results.Where(c => c.Type == SelectedCompetitionType));
		IsBusy = false;
		OnPropertyChanged(nameof(StoredCompetitionsForSelectedType));

	}

	/// <summary> Enable reloading from OnNavigatedTo (when db is reset from another page) </summary>
	[ICommand]
	public async Task SelectedCompetitionChanged()
	{
		AppShell.SetGamesVisible(SelectedCompetition is not null);
		if (SelectedCompetition is null)
		{
			return;
		}

		IsBusy = true;
		await OpenCompetition(SelectedCompetition);
		Log.Debug("Selected competition changed");
		IsBusy = false;

	}
}
