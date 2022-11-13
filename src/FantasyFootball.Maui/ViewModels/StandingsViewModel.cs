namespace FantasyFootball.ViewModels;

public partial class StandingsViewModel : GeneralViewModel
{
	readonly Dictionary<CompetitionType, List<TeamRecordViewModel>> _standingsCache = new() { [CompetitionType.EM] = new(), [CompetitionType.WM] = new(), };

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(OverallRecords))]
	List<Competition> _allCompetitions = new();

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(OverallRecords))]
	[NotifyPropertyChangedFor(nameof(CompetitionLogo))]
	CompetitionType _selectedCompetitionType = CompetitionType.EM;

	[ObservableProperty] Competition _selectedCompetition;

	public StandingsViewModel()
	{
		// TODO Revisit, this is really expensive, especially during batch simulation
		MessagingCenter.Subscribe<Game>(this, MessageKeys.GameFinished, g => UpdateStandings(g.Round.Stage.Competition, g));
		MessagingCenter.Subscribe<CompetitionSimulator>(this, MessageKeys.CompetitionFinished, async _ => await LoadAllCompetitions());
		_ = LoadAllCompetitions();
	}

	public IList<CompetitionType> CompetitionTypes { get; } = Enum.GetValues(typeof(CompetitionType)).Cast<CompetitionType>().ToList();

	public List<RecordsGroup> RecordsByGroup { get; set; } = new();

	public List<TeamRecordViewModel> OverallRecords => _standingsCache[SelectedCompetitionType];

	public ImageSource CompetitionLogo => IconStrings.GetCompetitionLogo(SelectedCompetitionType);

	public void UpdateStandings(Competition competition, Game? justFinished = null)
	{
		RecordsByGroup = new(competition.Groups.Select(group => new RecordsGroup(group.Name, group.GetStandings().Select(r => new TeamRecordViewModel(r, GetColor(r))))));
		OnPropertyChanged(nameof(RecordsByGroup));

		Color? GetColor(TeamRecord r) => r.Team.Equals(justFinished?.HomeTeam) || r.Team.Equals(justFinished?.AwayTeam) ? ResourceConstants.DefaultHighlightColor : null;
	}

	public async Task LoadAllCompetitions()
	{
		var competitionsFromDb = (await Repo.GetAllAsync<Competition>()).Where(c => c.IsFinished).ToList();

		foreach (var competitionType in CompetitionTypes)
		{
			var relevantGames = competitionsFromDb.Where(c => c.Type == competitionType).SelectMany(c => c.GamesByDate);
			var records = Standings.CreateFrom(relevantGames);
			var recordVms = records.Select(r => new TeamRecordViewModel(r)).ToList();

			_standingsCache[competitionType] = recordVms;
		}

		AllCompetitions = competitionsFromDb;
	}
}
