namespace FantasyFootball.ViewModels;

[QueryProperty(nameof(SelectedCompetitionType), nameof(SelectedCompetitionType))]
public partial class CompetitionsViewModel : GeneralViewModel
{
	public ObservableCollection<Competition> StoredCompetitionsForSelectedType => new(DataStore.GetAll<Competition>().Where(c => c.Type == SelectedCompetitionType));

	public IList<CompetitionType> CompetitionTypes { get; } = Enum.GetValues(typeof(CompetitionType)).Cast<CompetitionType>().ToList();

	// TODO Has no influence yet - remove or use
	public IList<int> Years { get; } = new[] { 2020, 2016 };
	public IList<TeamSelectionType> ParticipantSelectionTypes { get; } = Enum.GetValues(typeof(TeamSelectionType)).Cast<TeamSelectionType>().ToList();

	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(StoredCompetitionsForSelectedType))]
	[AlsoNotifyChangeFor(nameof(CompetitionLogo))]
	CompetitionType _selectedCompetitionType = CompetitionType.EM;

	[ObservableProperty]
	TeamSelectionType _selectedParticipantMode = TeamSelectionType.HISTORIC;

	public string SelectedCompetitionWinner => Res.InProgress;
	[ObservableProperty]
	int _selectedYear = 2020;

	public ImageSource CompetitionLogo => IconStrings.GetCompetitionLogo(SelectedCompetitionType);

	CompetitionFactory _competitionFactory;

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
		var competition = await CompetitionFactories.CreateEm(DataStore, SelectedParticipantMode);
		DataStore.Save(competition);
		Log.Debug("Competition created");
		ServiceHelper.GetService<StandingsViewModel>()!.LoadCompetition(competition);
		await OpenCompetition(competition.Id);
		IsBusy = false;
	}

	/// <summary> Enable reloading from OnNavigatedTo (when db is reset from another page) </summary>
	public void ReloadCompetitions() => OnPropertyChanged(nameof(StoredCompetitionsForSelectedType));
}
