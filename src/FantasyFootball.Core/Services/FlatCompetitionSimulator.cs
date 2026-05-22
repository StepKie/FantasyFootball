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

	/// <summary>
	/// Score a single game in place. Resolves KO qualifier chains as needed.
	/// No-op if the game already has a Result. Caller ensures upstream games
	/// are played first — KO resolution depends on group standings.
	/// Stamps <c>SimulationStart</c> on first call (any sim entry point;
	/// list views can tell scheduled from in-progress competitions).
	/// </summary>
	public void SimulateGame(FlatCompetition c, FlatGame game)
	{
		if (game.Result is not null) { return; }

		c.SimulationStart ??= DateTime.UtcNow;
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

	/// <summary>
	/// Score every unplayed game in the given round, chronological order.
	/// </summary>
	public void SimulateRound(FlatCompetition c, string roundId)
	{
		var roundGames = c.Games
			.Where(g => g.RoundId == roundId && g.Result is null)
			.OrderBy(g => g.PlayedOn);
		foreach (var game in roundGames)
		{
			SimulateGame(c, game);
		}
	}

	public void Simulate(FlatCompetition c)
	{
		c.SimulationStart ??= DateTime.UtcNow;

		// Defensive sort: relying on upstream order would foot-gun on a
		// hand-edited out-of-order definition. Cheap at our scale.
		foreach (var game in c.Games.OrderBy(g => g.PlayedOn))
		{
			SimulateGame(c, game);
		}

		c.SimulationFinished = DateTime.UtcNow;
	}

	static void ResolveKoTeams(FlatCompetition c, FlatKoGame ko)
	{
		// Cache resolved IDs so the qualifier walk runs at most once
		// per slot. Idempotent: a second call after first resolution is a no-op.
		ko.HomeTeamId ??= FlatQualifierResolver.Resolve(c, ko.HomeQual);
		ko.AwayTeamId ??= FlatQualifierResolver.Resolve(c, ko.AwayQual);
	}

	/// <summary>
	/// Best-effort pass: resolves <see cref="FlatKoGame.HomeTeamId"/> /
	/// <see cref="FlatKoGame.AwayTeamId"/> on every KO game whose qualifier
	/// chain is now satisfiable, so future-round games can display real team
	/// names + flags as soon as their group / earlier-KO prerequisites land.
	/// Silent on slots whose prerequisites aren't ready yet.
	/// </summary>
	public static void ResolveAvailableKoTeams(FlatCompetition c)
	{
		foreach (var ko in c.Games.OfType<FlatKoGame>())
		{
			ko.HomeTeamId ??= FlatQualifierResolver.TryResolve(c, ko.HomeQual);
			ko.AwayTeamId ??= FlatQualifierResolver.TryResolve(c, ko.AwayQual);
		}
	}
}
