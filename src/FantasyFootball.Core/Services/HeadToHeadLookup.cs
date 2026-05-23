using FantasyFootball.Models;
using FantasyFootball.Repositories;

namespace FantasyFootball.Services;

/// <summary>
/// Cross-competition head-to-head tally for the Competition store.
/// Walks every stored Competition's finished games once. Matches teams
/// by their ShortName id — works regardless of whether games are KO (with
/// resolved <c>HomeTeamId</c> / <c>AwayTeamId</c>) or group (with init-only ids).
/// </summary>
public static class HeadToHeadLookup
{
	public readonly record struct Result(int TeamAWins, int Draws, int TeamBWins)
	{
		public int Total => TeamAWins + Draws + TeamBWins;
	}

	public static async Task<Result> AcrossAsync(ICompetitionRepository repo, string teamAId, string teamBId)
	{
		var all = await repo.GetAllAsync();
		int aWins = 0, bWins = 0, draws = 0;

		foreach (var comp in all)
		{
			foreach (var game in comp.Games)
			{
				if (game.Result is not { } r) { continue; }
				var home = CompetitionExtensions.HomeTeamIdOf(game);
				var away = CompetitionExtensions.AwayTeamIdOf(game);
				if (home is null || away is null) { continue; }

				bool isAB = home == teamAId && away == teamBId;
				bool isBA = home == teamBId && away == teamAId;
				if (!isAB && !isBA) { continue; }

				if (r.HomeScore == r.AwayScore) { draws++; }
				else
				{
					var homeWon = r.HomeScore > r.AwayScore;
					var aWon = (isAB && homeWon) || (isBA && !homeWon);
					if (aWon) { aWins++; } else { bWins++; }
				}
			}
		}

		return new Result(aWins, draws, bWins);
	}
}
