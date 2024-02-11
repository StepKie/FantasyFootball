namespace FantasyFootball.ViewModels;

[QueryProperty(nameof(SelectedCompetitionType), nameof(SelectedCompetitionType))]
public partial class CompetitionsViewModel : GeneralViewModel
{
	public List<Competition> StoredCompetitionsForSelectedType { get; private set; } = [];

	// TODO Support remaining types
	public IList<CompetitionType> CompetitionTypes { get; } = [CompetitionType.WM, CompetitionType.EM];

	public IList<int> Years => SelectedCompetitionType.AvailableYears().ToList();

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(StoredCompetitionsForSelectedType))]
	[NotifyPropertyChangedFor(nameof(CompetitionLogo))]
	[NotifyPropertyChangedFor(nameof(Years))]
	[NotifyPropertyChangedFor(nameof(SelectedYear))]
	CompetitionType _selectedCompetitionType = CompetitionType.EM;

	[ObservableProperty]
	int _selectedYear;

	[ObservableProperty]
	Competition? _selectedCompetition;

	[ObservableProperty]
	int _defaultAmountOfBatchSimulations = 5;

	public CompetitionsViewModel()
	{
		MessageBus.Register<CompetitionFinishedMessage>(this, async (_, _) => await ReloadCompetitions());
		_ = ReloadCompetitions();
	}

	public ImageSource CompetitionLogo => IconStrings.GetCompetitionLogo(SelectedCompetitionType);

	[RelayCommand]
	async Task OpenCompetition(Competition competition)
	{
		ServiceHelper.GetService<StandingsViewModel>()!.UpdateStandings(competition);
		var route = $"//PlayTab/{nameof(GamesPage)}?{nameof(GamesViewModel.CompetitionId)}={competition.Id}";
		await Shell.Current.GoToAsync(route);
	}

	[RelayCommand]
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
	[RelayCommand]
	public async Task ReloadCompetitions()
	{
		IsBusy = true;
		var results = await Repo.GetAllAsync<Competition>();
		StoredCompetitionsForSelectedType = new(results.Where(c => c.Type == SelectedCompetitionType));
		IsBusy = false;
		OnPropertyChanged(nameof(StoredCompetitionsForSelectedType));

	}

	/// <summary> Enable reloading from OnNavigatedTo (when db is reset from another page) </summary>
	[RelayCommand]
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
