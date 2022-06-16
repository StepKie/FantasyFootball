namespace FantasyFootball.ViewModels;

public partial class StandingsViewModel : GeneralViewModel
{
	[ObservableProperty] CompetitionType _selectedCompetitionType;
	[ObservableProperty] Competition _selectedCompetition;

	public StandingsViewModel()
	{
		MessagingCenter.Subscribe<Game>(this, MessageKeys.GameFinished, UpdateStandings);
		// This is not called during initialization?!
		OnSelectedCompetitionTypeChanged(SelectedCompetitionType);
	}

	public IList<CompetitionType> CompetitionTypes { get; } = Enum.GetValues(typeof(CompetitionType)).Cast<CompetitionType>().ToList();

	public ObservableCollection<RecordsGroup> RecordsByGroup { get; private set; }
	public IList<TeamRecordViewModel> Records { get; set; }

	public ImageSource CompetitionLogo => IconStrings.GetCompetitionLogo(SelectedCompetitionType);

	partial void OnSelectedCompetitionTypeChanged(CompetitionType value)
	{
		var competitions = Repo.GetAll<Competition>().Where(c => c.Type == value);
		Title = $"{competitions.Count()} {SelectedCompetitionType}s";
		var competitionTypeGameHistory = competitions.SelectMany(c => c.GamesByDate).ToList();
		Log.Debug($"Set competition type to {Title}, found {competitionTypeGameHistory.Count} relevant games");
		Records = Standings.CreateFrom(competitionTypeGameHistory).Select(r => new TeamRecordViewModel(r, ResourceConstants.DefaultHighlightColor)).ToList();
		OnPropertyChanged(nameof(Records));
		OnPropertyChanged(nameof(CompetitionLogo));
	}

	public void UpdateStandings(Game justFinished)
	{
		Title = "";
		RecordsByGroup = new(justFinished.Round.Stage.Groups.Select(group => new RecordsGroup(group.Name, Standings.CreateFrom(group.Games).Select(r => new TeamRecordViewModel(r, GetColor(r))))));
		OnPropertyChanged(nameof(RecordsByGroup));

		Color GetColor(TeamRecord r) => r.Team.Equals(justFinished?.HomeTeam) || r.Team.Equals(justFinished?.AwayTeam) ? ResourceConstants.DefaultHighlightColor : ResourceConstants.DefaultPageColor;
	}

	public virtual void LoadCompetition(Competition competition)
	{
		try
		{
			RecordsByGroup = new(competition.Groups.Select(group => new RecordsGroup(group.Name, Standings.CreateFrom(group.Games).Select(r => new TeamRecordViewModel(r, ResourceConstants.DefaultPageColor)))));
			OnPropertyChanged(nameof(Records));
			OnPropertyChanged(nameof(RecordsByGroup));
		}
		catch (Exception e)
		{
			Log.Error($"Failed to load competition: {e}");
		}
	}
}
