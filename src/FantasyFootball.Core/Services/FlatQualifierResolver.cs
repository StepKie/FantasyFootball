using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Resolves a qualifier DSL string against a partially-simulated
/// <see cref="FlatCompetition"/>, producing the concrete team ID that
/// fills the slot. Walked by the simulator just before each KO game
/// to fix <c>HomeTeamId</c> / <c>AwayTeamId</c>.
///
/// Throws when the qualifier's referenced games / standings aren't
/// available yet — the simulator walks games chronologically, so by
/// the time a qualifier is resolved its inputs should already be
/// populated. A late call is a bug, not a runtime expectation.
/// </summary>
public static class FlatQualifierResolver
{
	public static string Resolve(FlatCompetition c, string qualifierDsl)
	{
		var q = FlatQualifierParser.Parse(qualifierDsl);
		return q switch
		{
			FlatGroupPlacement gp => ResolveGroupPlacement(c, gp),
			FlatGameWinner gw => ResolveGameOutcome(c, gw.GameId, winner: true),
			FlatGameLoser gl => ResolveGameOutcome(c, gl.GameId, winner: false),
			FlatThirdPlacePool pool => ResolveThirdPlacePool(c, pool),
			_ => throw new InvalidOperationException($"Unknown qualifier type {q.GetType().Name} for '{qualifierDsl}'."),
		};
	}

	static string ResolveGroupPlacement(FlatCompetition c, FlatGroupPlacement gp)
	{
		var standings = c.Standings(gp.GroupLetter);
		if (gp.Place < 1 || gp.Place > standings.Count)
		{
			throw new InvalidOperationException(
				$"Qualifier '{gp.GroupLetter}{gp.Place}' wants position {gp.Place}, but group '{gp.GroupLetter}' has {standings.Count} teams.");
		}
		return standings[gp.Place - 1].TeamId;
	}

	static string ResolveGameOutcome(FlatCompetition c, int gameId, bool winner)
	{
		var game = c.Games.FirstOrDefault(g => g.Id == gameId)
			?? throw new InvalidOperationException($"Qualifier references game {gameId}, which doesn't exist in competition '{c.DefinitionId}'.");
		if (game.Result is not { } r)
		{
			throw new InvalidOperationException(
				$"Qualifier references game {gameId}, which hasn't been played yet — simulator should run games chronologically.");
		}
		var home = FlatCompetitionExtensions.HomeTeamIdOf(game)
			?? throw new InvalidOperationException($"Game {gameId} has no resolved home team — qualifier chain broken upstream.");
		var away = FlatCompetitionExtensions.AwayTeamIdOf(game)
			?? throw new InvalidOperationException($"Game {gameId} has no resolved away team — qualifier chain broken upstream.");

		var homeWon = r.HomeScore > r.AwayScore;
		// Ties shouldn't reach this code path for KO games (the simulator
		// guarantees a winner via ET/penalties). If we ever see a tied
		// regular game here, the qualifier DSL is being misused.
		if (r.HomeScore == r.AwayScore)
		{
			throw new InvalidOperationException(
				$"Game {gameId} ended in a tie ({r.HomeScore}-{r.AwayScore}); winner/loser qualifiers only apply to games with a decisive result.");
		}
		return (winner == homeWon) ? home : away;
	}

	static string ResolveThirdPlacePool(FlatCompetition c, FlatThirdPlacePool pool)
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
