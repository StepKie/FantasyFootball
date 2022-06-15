namespace FantasyFootball.ViewModels;

public partial class GameViewModel : GeneralViewModel
{
	private readonly ImageSource _questionMark = new FontImageSource() { Glyph = IconFont.Question_mark, FontFamily = "MaterialIcons", Color = Colors.Grey };
	[ObservableProperty]
	Color _bgColor;

	public Game Game { get; init; }

	public string HomeName => Game.HomeTeam?.Name ?? Game.HomeTeamTentative;
	public string AwayName => Game.AwayTeam?.Name ?? Game.AwayTeamTentative;

	public ImageSource HomeLogo => Game.HomeTeam is not null ? Game.HomeTeam.Logo : _questionMark;
	public ImageSource AwayLogo => Game.AwayTeam is not null ? Game.AwayTeam.Logo : _questionMark;

	public GameViewModel(Game game)
	{
		Game = game;
		MessagingCenter.Subscribe<Game>(this, MessageKeys.GameFinished, game => UpdateUI(game));
	}

	void UpdateUI(Game justFinished)
	{
		BgColor = Game.Equals(justFinished) ? ResourceConstants.DefaultHighlightColor : ResourceConstants.DefaultPageColor;
		OnPropertyChanged(nameof(HomeName));
		OnPropertyChanged(nameof(AwayName));
		OnPropertyChanged(nameof(HomeLogo));
		OnPropertyChanged(nameof(AwayLogo));
		OnPropertyChanged(nameof(Game));
	}
}
