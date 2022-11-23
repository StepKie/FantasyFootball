namespace FantasyFootball.ViewModels;

public partial class GameViewModel : GeneralViewModel
{
	[ObservableProperty]
	Color _bgColor;

	public Game Game { get; init; }

	public string HomeName => Game.HomeTeam.Name;
	public string AwayName => Game.AwayTeam.Name;

	public ImageSource HomeLogo => Game.HomeTeam.Logo;
	public ImageSource AwayLogo => Game.AwayTeam.Logo;

	public GameViewModel(Game game)
	{
		Game = game;
		MessagingCenter.Subscribe<Game>(this, MessageKeys.GameFinished, UpdateUI);
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
