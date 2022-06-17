namespace FantasyFootball.Data;

//TODO Refactor for reuse in other tournaments
public class WorldCupRoundAdvancer
{
	public Competition Competition { get; init; }

	public WorldCupRoundAdvancer(Competition competition) => Competition = competition;

	public void CheckAdvanceRound(Game game)
	{
		// If it was the last game in a group stage, fill the subsequent K.O. stage
		if (game.Equals(Competition.Stages.First().Games.Last()))
		{
			var koGames = FillKoStage();
			if (koGames.FirstOrDefault(g => !g.IsReadyToStart) is Game notInitialized)
			{
				Log.Error($"{notInitialized} not initalized");
			}
		}

		// If it was a game in a K.O. Round which was not the final, advance the winner to the next round
		if (game.IsKo && game.Round.NextRoundInStage != null)
		{
			AdvanceInKoRound(game);
		}
	}

	void AdvanceInKoRound(Game game)
	{

		if (!(game.IsKo && game.IsFinished)) { throw new ArgumentException($"{game} is not finished or not K.O. game", nameof(game)); }

		var OrderInCurrentRound = game.Round.Games.IndexOf(game);
		var noInNextRound = OrderInCurrentRound / 2;
		var nextRoundGame = game.Round.NextRoundInStage!.Games[noInNextRound];
		nextRoundGame?.AddParticipant(game.Winner!);
	}

	IList<Game> FillKoStage()
	{
		// TODO Check validity!
		var groupStage = Competition.Stages[0];
		var firstKoRound = Competition.Stages[1].Rounds[0];
		var gamesInKoStage = firstKoRound.Games;

		if (!groupStage.IsFinished || gamesInKoStage.Any(g => g.IsFinished))
		{
			throw new ArgumentException("Ko stage should be filled when group stage is finished and no K.O. games have been played!");
		}

		var groups = groupStage.Groups;

		gamesInKoStage[0].AddParticipants(GetQualifier("A", 1), GetQualifier("B", 2));
		gamesInKoStage[1].AddParticipants(GetQualifier("C", 1), GetQualifier("D", 2));
		gamesInKoStage[2].AddParticipants(GetQualifier("D", 1), GetQualifier("C", 2));
		gamesInKoStage[3].AddParticipants(GetQualifier("B", 1), GetQualifier("A", 2));
		gamesInKoStage[4].AddParticipants(GetQualifier("E", 1), GetQualifier("F", 2));
		gamesInKoStage[5].AddParticipants(GetQualifier("G", 1), GetQualifier("H", 2));
		gamesInKoStage[6].AddParticipants(GetQualifier("F", 1), GetQualifier("E", 2));
		gamesInKoStage[7].AddParticipants(GetQualifier("H", 1), GetQualifier("G", 2));

		return gamesInKoStage;

		Team? GetQualifier(string groupLetter, int place) => groups.Find(g => g.Name.EndsWith(groupLetter))?.GetStandings()[place - 1].Team;
	}
}
