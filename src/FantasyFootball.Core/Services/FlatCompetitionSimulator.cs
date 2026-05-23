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

		// Stamp SimulationFinished here so manually-completed comps don't leave it null (bulk Simulate stamps post-loop).
		if (c.Games.All(g => g.Result is not null))
		{
			c.SimulationFinished ??= DateTime.UtcNow;
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

		// Defensive sort: a hand-edited out-of-order definition file would foot-gun without this.
		foreach (var game in c.Games.OrderBy(g => g.PlayedOn))
		{
			SimulateGame(c, game);
		}

		c.SimulationFinished = DateTime.UtcNow;
	}

	static void ResolveKoTeams(FlatCompetition c, FlatKoGame ko)
	{
		// Pool slots resolve as a batch; refresh before per-slot Resolve to guarantee they're populated.
		if (ko.HomeTeamId is null || ko.AwayTeamId is null) { ResolveAvailableKoTeams(c); }
		ko.HomeTeamId ??= FlatQualifierResolver.Resolve(c, ko.HomeQual);
		ko.AwayTeamId ??= FlatQualifierResolver.Resolve(c, ko.AwayQual);
	}

	/// <summary>
	/// Best-effort pass over every KO game. Non-pool qualifiers
	/// (<see cref="FlatGroupPlacement"/>, <see cref="FlatGameWinner"/>,
	/// <see cref="FlatGameLoser"/>) resolve per-slot via <see cref="FlatQualifierResolver"/>.
	/// 3rd-place pools resolve as a batch via <see cref="ResolveThirdPlacePools"/>
	/// — a single team per slot, no duplicates across overlapping pools.
	/// Silent on slots whose prerequisites aren't ready.
	/// </summary>
	public static void ResolveAvailableKoTeams(FlatCompetition c)
	{
		// Hot-path skip: all KO slots already assigned → nothing to do.
		var anyUnresolved = false;
		foreach (var ko in c.Games.OfType<FlatKoGame>())
		{
			if (ko.HomeTeamId is null || ko.AwayTeamId is null) { anyUnresolved = true; break; }
		}
		if (!anyUnresolved) { return; }

		foreach (var ko in c.Games.OfType<FlatKoGame>())
		{
			if (ko.HomeTeamId is null && IsNonPool(ko.HomeQual))
			{
				ko.HomeTeamId = FlatQualifierResolver.TryResolve(c, ko.HomeQual);
			}
			if (ko.AwayTeamId is null && IsNonPool(ko.AwayQual))
			{
				ko.AwayTeamId = FlatQualifierResolver.TryResolve(c, ko.AwayQual);
			}
		}
		ResolveThirdPlacePools(c);
	}

	static bool IsNonPool(string dsl) =>
		FlatQualifierParser.TryParse(dsl, out var q) && q is not FlatThirdPlacePool;

	/// <summary>
	/// Batch-assign 3rd-place pool slots. Globally ranks 3rd-placers across all groups
	/// referenced by any pool slot, takes the top N (where N = number of pool slots),
	/// and greedily assigns each to a slot whose <see cref="FlatThirdPlacePool.EligibleGroups"/>
	/// includes the team's group letter.
	///
	/// Only fills NULL slots — preserves existing assignments. Subsequent calls hit
	/// an empty <c>poolSlots</c> and return immediately, keeping the post-sim refresh
	/// path cheap on every game tick.
	/// </summary>
	static void ResolveThirdPlacePools(FlatCompetition c)
	{
		var poolSlots = new List<(FlatKoGame Game, bool IsHome, FlatThirdPlacePool Pool)>();
		foreach (var ko in c.Games.OfType<FlatKoGame>())
		{
			if (ko.HomeTeamId is null
				&& FlatQualifierParser.TryParse(ko.HomeQual, out var qh)
				&& qh is FlatThirdPlacePool ph)
			{
				poolSlots.Add((ko, true, ph));
			}
			if (ko.AwayTeamId is null
				&& FlatQualifierParser.TryParse(ko.AwayQual, out var qa)
				&& qa is FlatThirdPlacePool pa)
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
		var remaining = new List<(FlatKoGame Game, bool IsHome, FlatThirdPlacePool Pool)>(poolSlots);
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
