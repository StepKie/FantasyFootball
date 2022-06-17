namespace FantasyFootball.ViewModels;

public partial class StandingsViewModel : GeneralViewModel
{
	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(OverallRecords))]
	List<Competition> _allCompetitions = new();

	[ObservableProperty]
	[AlsoNotifyChangeFor(nameof(OverallRecords))]
	[AlsoNotifyChangeFor(nameof(CompetitionLogo))]
	CompetitionType _selectedCompetitionType = CompetitionType.EM;

	[ObservableProperty] Competition _selectedCompetition;

	public StandingsViewModel()
	{
		// TODO Revisit, this is really expensive, especially during batch simulation
		MessagingCenter.Subscribe<Game>(this, MessageKeys.GameFinished, g => UpdateStandings(g.Round.Stage.Competition, g));
		LoadAllCompetitions();
	}

	public IList<CompetitionType> CompetitionTypes { get; } = Enum.GetValues(typeof(CompetitionType)).Cast<CompetitionType>().ToList();

	public List<RecordsGroup> RecordsByGroup { get; set; } = new();

	public List<TeamRecordViewModel> OverallRecords
	{
		get
		{
			var result = Standings.CreateFrom(AllCompetitions
				.Where(c => c.Type == SelectedCompetitionType)
				.SelectMany(c => c.GamesByDate))
				.Select(r => new TeamRecordViewModel(r, ResourceConstants.DefaultPageColor)).ToList();
			return result;
		}
	}

	public ImageSource CompetitionLogo => IconStrings.GetCompetitionLogo(SelectedCompetitionType);

	public void UpdateStandings(Competition competition, Game? justFinished = null)
	{
		RecordsByGroup = new(competition.Groups.Select(group => new RecordsGroup(group.Name, Standings.CreateFrom(group.Games).Select(r => new TeamRecordViewModel(r, GetColor(r))))));
		OnPropertyChanged(nameof(RecordsByGroup));

		Color? GetColor(TeamRecord r) => r.Team.Equals(justFinished?.HomeTeam) || r.Team.Equals(justFinished?.AwayTeam) ? ResourceConstants.DefaultHighlightColor : null;
	}

	public void LoadAllCompetitions()
	{
		AllCompetitions = Repo.GetAll<Competition>().ToList();
	}
}
