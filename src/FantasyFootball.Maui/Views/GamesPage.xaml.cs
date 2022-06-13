namespace FantasyFootball.Views;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class GamesPage : ContentPage
{
	public GamesPage(GamesViewModel gamesViewModel)
	{
		InitializeComponent();
		BindingContext = gamesViewModel;
		MessagingCenter.Subscribe<Game>(this, MessageKeys.GameFinished, game => ScrollToGame(game));
	}

	void ScrollToGame(Game game)
	{
		GamesViewModel gvm = (BindingContext as GamesViewModel)!;
		var gamesByRound = gvm.GamesByRound;
		_ = gamesByRound ?? throw new MemberAccessException("Did not find gamesByRound as gamesCollection.ItemSource");
		var round = game.Round;
		var roundIndex = gvm.Competition.Rounds.IndexOf(round);
		var roundGroup = gamesByRound[roundIndex];
		var gameIndex = roundGroup.FindIndex(gvm => gvm.Game.Id == game.Id);
		var target = roundGroup[gameIndex];

		gamesCollection.ScrollTo(target, roundGroup, ScrollToPosition.Center);
	}
}
