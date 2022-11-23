namespace FantasyFootball.Views;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class GamesPage : ContentPage
{
	public GamesPage(GamesViewModel gamesViewModel)
	{
		InitializeComponent();
		BindingContext = gamesViewModel;
		MessagingCenter.Subscribe<Game>(this, MessageKeys.GameFinished, ScrollToGame);
	}

	void ScrollToGame(Game game)
	{
		GamesViewModel gvm = (BindingContext as GamesViewModel)!;

		var roundIndex = gvm.Competition.Rounds.IndexOf(game.Round);
		// Nothing to scroll if we get notified from a background thread for another competition
		if (roundIndex == -1) { return; }
		var roundGroup = gvm.GamesByRound[roundIndex];
		var gameIndex = roundGroup.FindIndex(gvm => gvm.Game.Id == game.Id);
		var target = roundGroup[gameIndex];

		gamesCollection.ScrollTo(target, roundGroup, ScrollToPosition.Center);
	}
}
