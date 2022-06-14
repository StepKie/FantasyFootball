﻿namespace FantasyFootball.Data;

//TODO Refactor for reuse in other tournaments
public class RoundAdvancer
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

	public RoundAdvancer(Competition competition) => Competition = competition;

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

		gamesInKoStage[0].AddParticipants(GetQualifier("B", 1), null);
		gamesInKoStage[1].AddParticipants(GetQualifier("A", 1), GetQualifier("C", 2));
		gamesInKoStage[2].AddParticipants(GetQualifier("F", 1), null);
		gamesInKoStage[3].AddParticipants(GetQualifier("D", 2), GetQualifier("E", 2));
		gamesInKoStage[4].AddParticipants(GetQualifier("E", 1), null);
		gamesInKoStage[5].AddParticipants(GetQualifier("D", 1), GetQualifier("F", 2));
		gamesInKoStage[6].AddParticipants(GetQualifier("C", 1), null);
		gamesInKoStage[7].AddParticipants(GetQualifier("A", 2), GetQualifier("B", 2));

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
			gamesInKoStage.First(g => !g.IsReadyToStart).AddParticipant(team);
		}

		return gamesInKoStage;

		Team? GetQualifier(string groupLetter, int place) => groups.Find(g => g.Name.EndsWith(groupLetter))?.GetStandings()[place - 1].Team;
	}
}
