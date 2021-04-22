using System.Reflection;

namespace FantasyFootball.ViewModels;

public partial class StandingsViewModel : GeneralViewModel
{
	[ObservableProperty] CompetitionType _selectedCompetitionType;

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
		// MessagingCenter.Subscribe<IRepository>(this, MessageKeys.CompetitionUpdated, _ => UpdateGamesFromCompetitionType());
		SelectedCompetitionType = CompetitionType.EM;
	}
	public StandingsViewModel(string title, IEnumerable<Game> games)
	{
		MessagingCenter.Subscribe<Game>(this, MessageKeys.GameFinished, justFinished => UpdateStandings(justFinished));
		Title = title;
		UpdateGames(games);
	}

	public void UpdateStandings(Game? justFinished)
	{
		Records = Standings.CreateFrom(Games).Select(r => new TeamRecordViewModel(r, GetColor(r))).ToList();
		OnPropertyChanged(nameof(Records));

		Color GetColor(TeamRecord r) => r.Team.Equals(justFinished?.HomeTeam) || r.Team.Equals(justFinished?.AwayTeam) ? ResourceDictionary.DefaultHighlightColor : Colors.White;
	}

	void UpdateGames(IEnumerable<Game> games)
	{
		Games = games;
		Records = Standings.CreateFrom(Games).Select(r => new TeamRecordViewModel(r, Colors.White)).ToList();
		OnPropertyChanged(nameof(Records));
	}
}
