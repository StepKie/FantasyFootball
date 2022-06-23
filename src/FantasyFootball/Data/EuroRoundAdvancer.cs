namespace FantasyFootball.Data;

//TODO Refactor for reuse in other tournaments
public class EuroRoundAdvancer
{
	public static readonly Dictionary<int[], int[]> thirdPlaceCombinations = new()
	{
		[new[] { 1, 2, 3, 4 }] = new[] { 1, 4, 2, 3 },
		[new[] { 1, 2, 3, 5 }] = new[] { 1, 5, 2, 3 },
		[new[] { 1, 2, 3, 6 }] = new[] { 1, 6, 2, 3 },
		[new[] { 1, 2, 4, 5 }] = new[] { 4, 5, 1, 2 },
		[new[] { 1, 2, 4, 6 }] = new[] { 4, 6, 1, 2 },
		[new[] { 1, 2, 5, 6 }] = new[] { 5, 6, 2, 1 },
		[new[] { 1, 3, 4, 5 }] = new[] { 5, 4, 3, 1 },
		[new[] { 1, 3, 4, 6 }] = new[] { 6, 4, 3, 1 },
		[new[] { 1, 3, 5, 6 }] = new[] { 5, 6, 3, 1 },
		[new[] { 1, 4, 5, 6 }] = new[] { 5, 6, 4, 1 },
		[new[] { 2, 3, 4, 5 }] = new[] { 5, 4, 2, 3 },
		[new[] { 2, 3, 4, 6 }] = new[] { 6, 4, 3, 2 },
		[new[] { 2, 3, 5, 6 }] = new[] { 6, 5, 3, 2 },
		[new[] { 2, 4, 5, 6 }] = new[] { 6, 5, 4, 2 },
		[new[] { 3, 4, 5, 6 }] = new[] { 6, 5, 4, 3 },

	};

	public Competition Competition { get; init; }

	public EuroRoundAdvancer(Competition competition) => Competition = competition;

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
		if (game is KoGame && game.Round.NextRoundInStage != null)
		{
			AdvanceInKoRound(game);
		}
	}

	static void AdvanceInKoRound(Game game)
	{

		if (!(game is KoGame && game.IsFinished)) { throw new ArgumentException($"{game} is not finished or not K.O. game", nameof(game)); }

		var OrderInCurrentRound = game.Round.RegularGames.IndexOf(game);
		var noInNextRound = OrderInCurrentRound / 2;
		var nextRoundGame = game.Round.NextRoundInStage!.RegularGames[noInNextRound];
		//nextRoundGame?.AddParticipant(game.Winner!);
	}

	IList<Game> FillKoStage()
	{
		// TODO Check validity!
		var groupStage = Competition.Stages[0];
		var firstKoRound = Competition.Stages[1].Rounds[0];
		var gamesInKoStage = firstKoRound.RegularGames;

		if (!groupStage.IsFinished || gamesInKoStage.Any(g => g.IsFinished))
		{
			throw new ArgumentException("Ko stage should be filled when group stage is finished and no K.O. games have been played!");
		}

		var groups = groupStage.Groups;

		var thirdPlaceFinishers = groups.Select(g => g.GetStandings()[2]).Select(tr => tr.Team);
		var bestFourThirdPlace = groups
			.SelectMany(g => g.GetStandings())
			.Where(tr => thirdPlaceFinishers.Contains(tr.Team))
			.OrderByDescending(tr => tr)
			.Take(4)
			.Select(tr => tr.Team);

		Log.Debug($"Best 4 3rd place finishers: {string.Join(",", bestFourThirdPlace)}");
		//TODO Assign correctly based on thirdPlaceCombinations

		foreach (var team in bestFourThirdPlace)
		{
			//For now, we cheat and assign randomly
			//gamesInKoStage.First(g => !g.IsReadyToStart).AddParticipant(team);
		}

		return gamesInKoStage;
	}
}
