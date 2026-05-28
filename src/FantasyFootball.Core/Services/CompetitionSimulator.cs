using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Simulates an entire <see cref="Competition"/> in one go. Walks
/// games in chronological order: group games score directly from the
/// <see cref="IScoreModel"/>, KO games resolve their qualifier chain
/// first (filling <c>HomeTeamId</c> / <c>AwayTeamId</c>) then score.
///
/// Each call mutates the supplied competition in place — Results are
/// filled in, <c>SimulationStart</c>/<c>SimulationFinished</c> are
/// stamped. Idempotent on a previously-finished competition: games
/// with an existing Result are not re-scored.
/// </summary>
public sealed class CompetitionSimulator
{
	readonly IScoreModel _scoreModel;

	public CompetitionSimulator(IScoreModel scoreModel)
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
	public void SimulateGame(Competition c, Game game, IScoreModel? scoreModelOverride = null)
	{
		if (game.Result is not null) { return; }

		var model = scoreModelOverride ?? _scoreModel;
		c.SimulationStart ??= DateTime.UtcNow;
		switch (game)
		{
			case GroupGame gg:
				gg.Result = model.ScoreGroupGame(gg.HomeTeamId, gg.AwayTeamId);
				break;
			case KoGame ko:
				ResolveKoTeams(c, ko);
				ko.Result = model.ScoreKoGame(ko.HomeTeamId!, ko.AwayTeamId!);
				break;
		}

		// Stamp SimulationFinished here so manually-completed comps don't leave it null (bulk Simulate stamps post-loop).
		if (c.Games.All(g => g.Result is not null))
		{
			c.SimulationFinished ??= DateTime.UtcNow;
		}
	}

	/// <summary>
	/// Score every unplayed game in the given round, chronological order.
	/// </summary>
	public void SimulateRound(Competition c, string roundId)
	{
		var roundGames = c.Games
			.Where(g => g.RoundId == roundId && g.Result is null)
			.OrderBy(g => g.PlayedOn);
		foreach (var game in roundGames)
		{
			SimulateGame(c, game);
		}
	}

	/// <param name="scoreModelOverride">
	/// Optional per-call score model. Used by the bulk runner to thread a
	/// year-matched <see cref="EloSet"/> through a HistoricalSpec sim without
	/// touching the UI's active set. <c>null</c> falls back to the DI default.
	/// </param>
	public void Simulate(Competition c, IScoreModel? scoreModelOverride = null)
	{
		c.SimulationStart ??= DateTime.UtcNow;

		// Defensive sort: a hand-edited out-of-order definition file would foot-gun without this.
		foreach (var game in c.Games.OrderBy(g => g.PlayedOn))
		{
			SimulateGame(c, game, scoreModelOverride);
		}

		c.SimulationFinished = DateTime.UtcNow;
	}

	static void ResolveKoTeams(Competition c, KoGame ko)
	{
		// Pool slots resolve as a batch; refresh before per-slot Resolve to guarantee they're populated.
		if (ko.HomeTeamId is null || ko.AwayTeamId is null) { ResolveAvailableKoTeams(c); }
		ko.HomeTeamId ??= QualifierResolver.Resolve(c, ko.HomeQual);
		ko.AwayTeamId ??= QualifierResolver.Resolve(c, ko.AwayQual);
	}

	/// <summary>
	/// Best-effort pass over every KO game. Non-pool qualifiers
	/// (<see cref="GroupPlacement"/>, <see cref="GameWinner"/>,
	/// <see cref="GameLoser"/>) resolve per-slot via <see cref="QualifierResolver"/>.
	/// 3rd-place pools resolve as a batch via <see cref="ResolveThirdPlacePools"/>
	/// — a single team per slot, no duplicates across overlapping pools.
	/// Silent on slots whose prerequisites aren't ready.
	/// </summary>
	public static void ResolveAvailableKoTeams(Competition c)
	{
		// Hot-path skip: all KO slots already assigned → nothing to do.
		var anyUnresolved = false;
		foreach (var ko in c.Games.OfType<KoGame>())
		{
			if (ko.HomeTeamId is null || ko.AwayTeamId is null) { anyUnresolved = true; break; }
		}
		if (!anyUnresolved) { return; }

		foreach (var ko in c.Games.OfType<KoGame>())
		{
			if (ko.HomeTeamId is null && IsNonPool(ko.HomeQual))
			{
				ko.HomeTeamId = QualifierResolver.TryResolve(c, ko.HomeQual);
			}
			if (ko.AwayTeamId is null && IsNonPool(ko.AwayQual))
			{
				ko.AwayTeamId = QualifierResolver.TryResolve(c, ko.AwayQual);
			}
		}
		ResolveThirdPlacePools(c);
	}

	static bool IsNonPool(string dsl) =>
		QualifierParser.TryParse(dsl, out var q) && q is not ThirdPlacePool;

	/// <summary>
	/// Batch-assign 3rd-place pool slots. Globally ranks 3rd-placers across all groups
	/// referenced by any pool slot, takes the top N (where N = number of pool slots),
	/// and greedily assigns each to a slot whose <see cref="ThirdPlacePool.EligibleGroups"/>
	/// includes the team's group letter.
	///
	/// Only fills NULL slots — preserves existing assignments. Subsequent calls hit
	/// an empty <c>poolSlots</c> and return immediately, keeping the post-sim refresh
	/// path cheap on every game tick.
	/// </summary>
	static void ResolveThirdPlacePools(Competition c)
	{
		var poolSlots = new List<(KoGame Game, bool IsHome, ThirdPlacePool Pool)>();
		foreach (var ko in c.Games.OfType<KoGame>())
		{
			if (ko.HomeTeamId is null
				&& QualifierParser.TryParse(ko.HomeQual, out var qh)
				&& qh is ThirdPlacePool ph)
			{
				poolSlots.Add((ko, true, ph));
			}
			if (ko.AwayTeamId is null
				&& QualifierParser.TryParse(ko.AwayQual, out var qa)
				&& qa is ThirdPlacePool pa)
			{
				poolSlots.Add((ko, false, pa));
			}
		}

		if (poolSlots.Count == 0) { return; }

		// All groups referenced by any pool slot must have completed games.
		var groupsNeeded = poolSlots.SelectMany(s => s.Pool.EligibleGroups).Distinct().ToList();
		foreach (var letter in groupsNeeded)
		{
			if (c.GroupGames(letter).Any(g => g.Result is null)) { return; }
		}

		// Global ranking of 3rd-placers, same tiebreaker cascade as within a group; ElementAtOrDefault guards groups with < 3 teams (matches sibling resolvers).
		var thirdPlacers = groupsNeeded
			.Select(g => (Letter: g, Standing: c.Standings(g).ElementAtOrDefault(2)))
			.Where(x => x.Standing is not null)
			.Select(x => (x.Letter, Standing: x.Standing!))
			.OrderByDescending(x => x.Standing.Points)
			.ThenByDescending(x => x.Standing.GoalDifference)
			.ThenByDescending(x => x.Standing.GoalsFor)
			.ThenBy(x => x.Standing.TeamId, StringComparer.Ordinal)
			.ToList();

		// Greedy best-team-first; if a team's letter isn't in any remaining slot's eligibles we skip it and try the next, until `remaining` is empty.
		var remaining = new List<(KoGame Game, bool IsHome, ThirdPlacePool Pool)>(poolSlots);
		foreach (var (letter, standing) in thirdPlacers)
		{
			if (remaining.Count == 0) { break; }
			var idx = remaining.FindIndex(s => s.Pool.EligibleGroups.Contains(letter));
			if (idx < 0) { continue; }
			var slot = remaining[idx];
			if (slot.IsHome) { slot.Game.HomeTeamId = standing.TeamId; }
			else { slot.Game.AwayTeamId = standing.TeamId; }
			remaining.RemoveAt(idx);
		}
	}
}
