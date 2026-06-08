using FantasyFootball.Models;
using FantasyFootball.Repositories;

namespace FantasyFootball.Services;

/// <summary>
/// Cross-competition head-to-head tally for the Competition store. Walks every
/// stored competition's finished games once, matching teams by their ShortName id —
/// works for both KO (resolved <c>HomeTeamId</c> / <c>AwayTeamId</c>) and group
/// (init-only id) games. The repo caches the bulk load, so repeat lookups are cheap.
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
				if (!game.IsFullyInitialized) { continue; }
				var home = game.HomeTeamId;
				var away = game.AwayTeamId;

				bool isAB = home == teamAId && away == teamBId;
				bool isBA = home == teamBId && away == teamAId;
				if (!isAB && !isBA) { continue; }

				if (r.IsDraw) { draws++; }
				else
				{
					var aWon = (isAB && r.HomeWon) || (isBA && r.AwayWon);
					if (aWon) { aWins++; } else { bWins++; }
				}
			}
		}

		return new Result(aWins, draws, bWins);
	}
}
