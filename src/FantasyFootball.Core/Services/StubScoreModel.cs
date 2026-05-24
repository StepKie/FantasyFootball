using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Deterministic placeholder score model: hashes the two team IDs into
/// a small score pair that's stable per (home, away) pairing but
/// varies across pairings. Good for tests that need every game played
/// without flaking, but not realistic — the ELO-based Poisson model
/// is the eventual production scorer.
///
/// KO games are tweaked to guarantee a non-draw (a fake 5-4 shootout
/// is appended with <see cref="GameEnd.PENALTIES"/> if the base scores
/// tied), satisfying the IScoreModel contract that KO games must be
/// decisive.
/// </summary>
public sealed class StubScoreModel : IScoreModel
{
	public Result ScoreGroupGame(string homeTeamId, string awayTeamId)
	{
		var (home, away) = StableScore(homeTeamId, awayTeamId);
		return new Result(home, away, GameEnd.NORMAL);
	}

	public Result ScoreKoGame(string homeTeamId, string awayTeamId)
	{
		var (home, away) = StableScore(homeTeamId, awayTeamId);
		// Forces a decision via a fixed 5-4 shootout; a realistic model would sim ET first then pens only if still tied.
		return home == away
			? new Result(home, away, GameEnd.PENALTIES, PenaltyHome: 5, PenaltyAway: 4)
			: new Result(home, away, GameEnd.NORMAL);
	}

	static (int Home, int Away) StableScore(string home, string away)
	{
		var hHash = (uint)home.GetHashCode();
		var aHash = (uint)away.GetHashCode();
		return ((int)(hHash % 5), (int)(aHash % 5));
	}
}
