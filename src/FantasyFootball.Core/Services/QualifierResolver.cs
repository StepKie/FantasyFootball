using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Resolves a qualifier DSL string against a partially-simulated
/// <see cref="Competition"/>, producing the concrete team ID that
/// fills the slot. Walked by the simulator just before each KO game
/// to fix <c>HomeTeamId</c> / <c>AwayTeamId</c>.
///
/// Throws when the qualifier's referenced games / standings aren't
/// available yet — the simulator walks games chronologically, so by
/// the time a qualifier is resolved its inputs should already be
/// populated. A late call is a bug, not a runtime expectation.
/// </summary>
public static class QualifierResolver
{
	/// <summary>
	/// Best-effort resolution: returns the team ID if the qualifier chain
	/// is fully resolvable (upstream games played, standings available);
	/// returns null if any prerequisite isn't ready yet.
	/// </summary>
	public static string? TryResolve(Competition c, string qualifierDsl)
	{
		// InvalidOperationException = prerequisite not yet played; FormatException/ArgumentException = bad DSL and stay loud.
		try { return Resolve(c, qualifierDsl); }
		catch (InvalidOperationException) { return null; }
	}

	public static string Resolve(Competition c, string qualifierDsl)
	{
		var q = QualifierParser.Parse(qualifierDsl);
		return q switch
		{
			GroupPlacement gp => ResolveGroupPlacement(c, gp),
			GameWinner gw => ResolveGameOutcome(c, gw.GameId, winner: true),
			GameLoser gl => ResolveGameOutcome(c, gl.GameId, winner: false),
			ThirdPlacePool pool => ResolveThirdPlacePool(c, pool),
			_ => throw new InvalidOperationException($"Unknown qualifier type {q.GetType().Name} for '{qualifierDsl}'."),
		};
	}

	static string ResolveGroupPlacement(Competition c, GroupPlacement gp)
	{
		var standings = c.Standings(gp.GroupLetter);
		if (gp.Place < 1 || gp.Place > standings.Count)
		{
			throw new InvalidOperationException(
				$"Qualifier '{gp.GroupLetter}{gp.Place}' wants position {gp.Place}, but group '{gp.GroupLetter}' has {standings.Count} teams.");
		}
		return standings[gp.Place - 1].TeamId;
	}

	static string ResolveGameOutcome(Competition c, int gameId, bool winner)
	{
		var game = c.Games.FirstOrDefault(g => g.Id == gameId)
			?? throw new InvalidOperationException($"Qualifier references game {gameId}, which doesn't exist in competition '{c.DefinitionId}'.");
		if (game.Result is not { } r)
		{
			throw new InvalidOperationException(
				$"Qualifier references game {gameId}, which hasn't been played yet — simulator should run games chronologically.");
		}
		var home = game.IsHomeInitialized
			? game.HomeTeamId
			: throw new InvalidOperationException($"Game {gameId} has no resolved home team — qualifier chain broken upstream.");
		var away = game.IsAwayInitialized
			? game.AwayTeamId
			: throw new InvalidOperationException($"Game {gameId} has no resolved away team — qualifier chain broken upstream.");

		// IsDraw covers regulation ties; the simulator guarantees ET/PENALTIES for KO games. A draw here means the DSL is misused on a group game.
		if (r.IsDraw)
		{
			throw new InvalidOperationException(
				$"Game {gameId} ended in a tie ({r.HomeScore}-{r.AwayScore}); winner/loser qualifiers only apply to games with a decisive result.");
		}
		return (winner == r.HomeWon) ? home : away;
	}

	static string ResolveThirdPlacePool(Competition c, ThirdPlacePool pool)
	{
		// Take the 3rd-place finisher from each eligible group, then
		// rank them across the pool using the same tiebreakers we use
		// within a group (points → GD → GF → team ID alphabetical).
		var ranked = pool.EligibleGroups
			.Select(g => c.Standings(g))
			.Select(static s =>
			{
				if (s.Count < 3)
				{
					throw new InvalidOperationException(
						$"Third-place pool needs a 3rd-place row from each eligible group, but a group has only {s.Count} teams.");
				}
				return s[2];
			})
			.OrderByDescending(s => s.Points)
			.ThenByDescending(s => s.GoalDifference)
			.ThenByDescending(s => s.GoalsFor)
			.ThenBy(s => s.TeamId, StringComparer.Ordinal)
			.ToList();
		return ranked[0].TeamId;
	}
}
