using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Deterministic placeholder score model: hashes the two team IDs into
/// a small score pair that's stable per (home, away) pairing but
/// varies across pairings. Good for tests that need every game played
/// without flaking, but not realistic — the ELO-based Poisson model
/// is the eventual production scorer.
///
/// KO games are tweaked to guarantee a non-draw (home gets one extra
/// goal in <see cref="GameEnd.PENALTIES"/> if the base scores tied),
/// satisfying the IScoreModel contract that KO games must be decisive.
/// </summary>
public sealed class StubScoreModel : IScoreModel
{
	public FlatResult ScoreGroupGame(string homeTeamId, string awayTeamId)
	{
		var (home, away) = StableScore(homeTeamId, awayTeamId);
		return new FlatResult(home, away, GameEnd.NORMAL);
	}

	public FlatResult ScoreKoGame(string homeTeamId, string awayTeamId)
	{
		var (home, away) = StableScore(homeTeamId, awayTeamId);
		if (home == away)
		{
			// Force a decision — modelled here as "home wins on penalties".
			// A realistic model would simulate ET first, then penalties only
			// if still tied.
			return new FlatResult(home + 1, away, GameEnd.PENALTIES);
		}
		return new FlatResult(home, away, GameEnd.NORMAL);
	}

	static (int Home, int Away) StableScore(string home, string away)
	{
		// Pseudo-score from team-id hashes, clamped to [0, 4]. Stable
		// across runs — same inputs give same scores.
		var hHash = (uint)home.GetHashCode();
		var aHash = (uint)away.GetHashCode();
		return ((int)(hHash % 5), (int)(aHash % 5));
	}
}
