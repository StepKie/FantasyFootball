namespace FantasyFootball.ViewModels;

public partial class GameViewModel : GeneralViewModel
{
	[ObservableProperty]
	Color _bgColor;

	public Game Game { get; init; }

	public string HomeName => Game.HomeTeam?.Name ?? Game.HomeTeamTentative;
	public string AwayName => Game.AwayTeam?.Name ?? Game.AwayTeamTentative;

	public GameViewModel(Game game)
	{
		Game = game;
		MessagingCenter.Subscribe<Game>(this, MessageKeys.GameFinished, game => UpdateUI(game));
	}

	void UpdateUI(Game justFinished)
	{
		BgColor = Game.Equals(justFinished) ? ResourceConstants.DefaultHighlightColor : Colors.White;
		OnPropertyChanged(nameof(HomeName));
		OnPropertyChanged(nameof(AwayName));
		OnPropertyChanged(nameof(Game));
	}
}
