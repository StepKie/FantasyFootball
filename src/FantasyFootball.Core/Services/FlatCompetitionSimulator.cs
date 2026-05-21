using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Simulates an entire <see cref="FlatCompetition"/> in one go. Walks
/// games in chronological order: group games score directly from the
/// <see cref="IScoreModel"/>, KO games resolve their qualifier chain
/// first (filling <c>HomeTeamId</c> / <c>AwayTeamId</c>) then score.
///
/// Each call mutates the supplied competition in place — Results are
/// filled in, <c>SimulationStart</c>/<c>SimulationFinished</c> are
/// stamped. Idempotent on a previously-finished competition: games
/// with an existing Result are not re-scored.
/// </summary>
public sealed class FlatCompetitionSimulator
{
	readonly IScoreModel _scoreModel;

	public FlatCompetitionSimulator(IScoreModel scoreModel)
	{
		_scoreModel = scoreModel;
	}

	public void Simulate(FlatCompetition c)
	{
		c.SimulationStart = DateTime.UtcNow;

		// Games are stored chronologically, but defensive sort: relying on
		// upstream ordering is a foot-gun if a definition file is hand-
		// edited out of order. Cheap at our scale.
		var ordered = c.Games.OrderBy(g => g.PlayedOn).ToList();

		for (int i = 0; i < ordered.Count; i++)
		{
			var game = ordered[i];
			if (game.Result is not null) { continue; }     // already played — skip

			switch (game)
			{
				case FlatGroupGame gg:
					gg.Result = _scoreModel.ScoreGroupGame(gg.HomeTeamId, gg.AwayTeamId);
					break;
				case FlatKoGame ko:
					ResolveKoTeams(c, ko);
					ko.Result = _scoreModel.ScoreKoGame(ko.HomeTeamId!, ko.AwayTeamId!);
					break;
			}
		}

		c.SimulationFinished = DateTime.UtcNow;
	}

	static void ResolveKoTeams(FlatCompetition c, FlatKoGame ko)
	{
		// Cache resolved IDs on the KO game so the qualifier walk runs
		// at most once per slot. Idempotent: a second call after first
		// resolution is a no-op.
		ko.HomeTeamId ??= FlatQualifierResolver.Resolve(c, ko.HomeQual);
		ko.AwayTeamId ??= FlatQualifierResolver.Resolve(c, ko.AwayQual);
	}
}
