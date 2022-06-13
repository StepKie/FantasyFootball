namespace FantasyFootball.ViewModels;

[QueryProperty(nameof(SelectedCompetitionType), nameof(SelectedCompetitionType))]
public partial class CompetitionsViewModel : GeneralViewModel
{
	public ObservableCollection<Competition> StoredCompetitionsForSelectedType => new(DataStore.GetAll<Competition>().Where(c => c.Type == SelectedCompetitionType));

	public IList<CompetitionType> CompetitionTypes { get; } = Enum.GetValues(typeof(CompetitionType)).Cast<CompetitionType>().ToList();
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
	async Task SimulateCompetition()
	{
		Log.Debug("Creating competition");
		_competitionFactory = new EmCompetitionFactory(DataStore);
		var competition = _competitionFactory.Create();
		DataStore.Save(competition);
		Log.Debug("Competition created");
		await OpenCompetition(competition.Id);
	}

	/// <summary> Enable reloading from OnNavigatedTo (when db is reset from another page) </summary>
	public void ReloadCompetitions() => OnPropertyChanged(nameof(StoredCompetitionsForSelectedType));
}
