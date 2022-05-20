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
		var gvm = BindingContext as GamesViewModel;
		if (gvm?.Competition.Id != game.Round.Stage.Competition.Id) { Log.Debug("Other game view, ignoring ..."); return; }
		var gamesByRound = gvm?.GamesByRound;
		_ = gamesByRound ?? throw new MemberAccessException("Did not find gamesByRound as gamesCollection.ItemSource");
		var round = game.Round;
		var roundIndex = round.Stage.Competition.Rounds.IndexOf(round);
		var roundGroup = gamesByRound[roundIndex];
		var gameIndex = roundGroup.FindIndex(gvm => gvm.Game.Id == game.Id);

		Log.Debug($"Scrolling to {game}, absolute index (game,round) is {gameIndex}, {roundIndex}");
		gamesCollection.ScrollTo(roundGroup[gameIndex], roundGroup);
		Log.Debug($"Scrolling complete");
	}
}
