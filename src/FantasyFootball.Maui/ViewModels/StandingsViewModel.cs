using System.Reflection;

namespace FantasyFootball.ViewModels;

public partial class StandingsViewModel : GeneralViewModel
{
	[ObservableProperty] CompetitionType _selectedCompetitionType;

	public ObservableCollection<RecordsGroup> RecordsByGroup { get; private set; }
	public IList<TeamRecordViewModel> Records { get; set; }
	public IList<Team> Teams { get; init; }

	public IEnumerable<Game> Games { get; set; }

	public ImageSource CompetitionLogo => ImageSource.FromResource(IconStrings.GetCompetitionLogo(SelectedCompetitionType), Assembly.GetExecutingAssembly());

	partial void OnSelectedCompetitionTypeChanged(CompetitionType value)
	{
		var competitions = DataStore.GetAll<Competition>().Where(c => c.Type == value);
		Title = $"{competitions.Count()} {SelectedCompetitionType}s";
		var games = competitions.SelectMany(c => c.GamesByDate).ToList();
		Log.Debug($"Set competition type to {Title}, found {games.Count} relevant games");
		UpdateGames(games);
		OnPropertyChanged(nameof(CompetitionLogo));
	}

	public IList<CompetitionType> CompetitionTypes { get; } = Enum.GetValues(typeof(CompetitionType)).Cast<CompetitionType>().ToList();

	public StandingsViewModel()
	{
		MessagingCenter.Subscribe<Game>(this, MessageKeys.GameFinished, UpdateStandings);
	}
	public StandingsViewModel(string title, IEnumerable<Game> games)
	{
		MessagingCenter.Subscribe<Game>(this, MessageKeys.GameFinished, UpdateStandings);
		Title = title;
		UpdateGames(games);
	}

	public void UpdateStandings(Game? justFinished)
	{
		RecordsByGroup = new(justFinished?.Round.Stage.Groups.Select(group => new RecordsGroup(group.Name, Standings.CreateFrom(group.Games).Select(r => new TeamRecordViewModel(r, GetColor(r))))));
		//var records = RecordsByGroup.SelectMany(r => r).Select(r => r.Record);
		// records.Where(r => r != null)
		OnPropertyChanged(nameof(RecordsByGroup));

		Color GetColor(TeamRecord r) => r.Team.Equals(justFinished?.HomeTeam) || r.Team.Equals(justFinished?.AwayTeam) ? ResourceDictionary.DefaultHighlightColor : Colors.White;
	}

	void UpdateGames(IEnumerable<Game> games)
	{
		Games = games;
		Records = Standings.CreateFrom(Games).Select(r => new TeamRecordViewModel(r, Colors.White)).ToList();
		OnPropertyChanged(nameof(Records));
	}

	public virtual void LoadCompetition(Competition competition)
	{
		try
		{
			RecordsByGroup = new(competition.Groups.Select(group => new RecordsGroup(group.Name, Standings.CreateFrom(group.Games).Select(r => new TeamRecordViewModel(r, Colors.White)))));
			OnPropertyChanged(nameof(RecordsByGroup));
		}
		catch (Exception e)
		{
			Log.Error($"Failed to load competition: {e}");
		}
	}
}
