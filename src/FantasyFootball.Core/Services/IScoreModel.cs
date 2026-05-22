using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Generates a <see cref="FlatResult"/> for one game given the two
/// participating team IDs. Decoupled from the simulator so we can swap
/// scoring strategies without touching simulation orchestration —
/// today a deterministic stub for tests, soon ELO-derived Poisson
/// sampling for realistic scores.
///
/// Implementations may be stateful (RNG, ELO lookup); pass a fresh
/// model per simulation if seed reproducibility is needed.
/// </summary>
public interface IScoreModel
{
	/// <summary>
	/// Score a group-stage game. Draws are allowed.
	/// </summary>
	FlatResult ScoreGroupGame(string homeTeamId, string awayTeamId);

	/// <summary>
	/// Score a KO-stage game. Must return a decisive result (no draw)
	/// — implementations can model extra time / penalties internally
	/// and set <see cref="FlatResult.Ending"/> accordingly.
	/// </summary>
	FlatResult ScoreKoGame(string homeTeamId, string awayTeamId);
}
