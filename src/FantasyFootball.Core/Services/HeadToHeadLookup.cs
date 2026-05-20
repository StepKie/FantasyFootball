using FantasyFootball.Repositories;

namespace FantasyFootball.Services;

/// <summary>
/// Cross-competition head-to-head tally. Walks every stored Competition's finished games once;
/// callers should treat this as a one-shot lookup (no caching).
/// </summary>
public static class HeadToHeadLookup
{
	public readonly record struct Result(int TeamAWins, int Draws, int TeamBWins)
	{
		public int Total => TeamAWins + Draws + TeamBWins;
	}

	public static Result Across(IRepository repo, Team teamA, Team teamB)
	{
		// Cross-competition: each competition deserializes its own Team instances. ReferenceEquals
		// (via NamedUniqueId.Equals on Id=0 entities) would never match across the graph boundary;
		// fall back to Name, which is stable. SQLite path with Id>0 would also match Name.
		var nameA = teamA.Name;
		var nameB = teamB.Name;
		var aWins = 0;
		var bWins = 0;
		var draws = 0;

		// Stream the games per competition: no date sort, no per-call list allocation.
		foreach (var comp in repo.GetAll<Competition>())
		{
			foreach (var stage in comp.Stages)
			foreach (var round in stage.Rounds)
			foreach (var game in round.AllGames)
			{
				if (!game.IsFinished) { continue; }

				var home = game.HomeTeam?.Name;
				var away = game.AwayTeam?.Name;
				var isMatchup = (home == nameA && away == nameB) || (home == nameB && away == nameA);
				if (!isMatchup) { continue; }

				var winnerName = game.Winner?.Name;
				if (winnerName is null) { draws++; }
				else if (winnerName == nameA) { aWins++; }
				else if (winnerName == nameB) { bWins++; }
				// else: winner is neither — unreachable today (isMatchup above guarantees it), but the explicit branch keeps the intent legible if Game ever gains a separate winner-track entity.
			}
		}

		return new Result(aWins, draws, bWins);
	}
}
