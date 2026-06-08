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
	/// <summary>
	/// Score a single game in place. Resolves KO qualifier chains as needed.
	/// No-op if the game already has a Result. Caller ensures upstream games
	/// are played first — KO resolution depends on group standings.
	/// Stamps <c>SimulationStart</c> on first call (any sim entry point;
	/// list views can tell scheduled from in-progress competitions).
	/// </summary>
	public void SimulateGame(Competition c, Game game, IScoreModel scoreModel)
	{
		if (game.Result is not null) { return; }

		c.SimulationStart ??= DateTime.UtcNow;
		switch (game)
		{
			case GroupGame gg:
				gg.Result = scoreModel.ScoreGroupGame(gg.HomeTeamId, gg.AwayTeamId);
				break;
			case LeagueGame lg:
				// Scored like a group game; no qualifier chain.
				lg.Result = scoreModel.ScoreGroupGame(lg.HomeTeamId, lg.AwayTeamId);
				break;
			case KoGame ko:
				ResolveKoTeams(c, ko);
				ko.Result = scoreModel.ScoreKoGame(ko.HomeTeamId, ko.AwayTeamId);
				break;
		}

		// Stamp SimulationFinished here so manually-completed comps don't leave it null (bulk Simulate stamps post-loop).
		if (c.Games.All(g => g.Result is not null))
		{
			c.SimulationFinished ??= DateTime.UtcNow;
		}
	}

	/// <summary>Score every unplayed game in the given round, chronological order.</summary>
	public void SimulateRound(Competition c, string roundId, IScoreModel scoreModel)
	{
		var roundGames = c.Games
			.Where(g => g.RoundId == roundId && g.Result is null)
			.OrderBy(g => g.PlayedOn);
		foreach (var game in roundGames)
		{
			SimulateGame(c, game, scoreModel);
		}
	}

	public void Simulate(Competition c, IScoreModel scoreModel)
	{
		c.SimulationStart ??= DateTime.UtcNow;

		// Defensive sort: a hand-edited out-of-order definition file would foot-gun without this.
		foreach (var game in c.Games.OrderBy(g => g.PlayedOn))
		{
			SimulateGame(c, game, scoreModel);
		}

		c.SimulationFinished = DateTime.UtcNow;
	}

	static void ResolveKoTeams(Competition c, KoGame ko)
	{
		// Pool slots resolve as a batch; refresh before per-slot Resolve to guarantee they're populated.
		if (!ko.IsFullyInitialized) { ResolveAvailableKoTeams(c); }
		if (!ko.IsHomeInitialized) { ko.HomeTeamId = QualifierResolver.Resolve(c, ko.HomeQual); }
		if (!ko.IsAwayInitialized) { ko.AwayTeamId = QualifierResolver.Resolve(c, ko.AwayQual); }
	}

	/// <summary>
	/// Best-effort pass over every KO game. For each <b>unplayed</b> KO game, non-pool
	/// qualifiers (<see cref="GroupPlacement"/>, <see cref="GameWinner"/>,
	/// <see cref="GameLoser"/>) re-resolve per-slot via <see cref="QualifierResolver"/>
	/// — the slot tracks the current standings/upstream-game outcome until that source
	/// is final, so R32 shows a live preview during group stage instead of locking to
	/// whoever was leading after game 1. Played KO games stay frozen.
	/// 3rd-place pools resolve as a batch via <see cref="ResolveThirdPlacePools"/>
	/// — a single team per slot, no duplicates across overlapping pools.
	/// Silent on slots whose prerequisites aren't ready.
	/// </summary>
	public static void ResolveAvailableKoTeams(Competition c)
	{
		foreach (var ko in c.Games.OfType<KoGame>())
		{
			// Played games are frozen — changing their teams now would orphan the recorded Result.
			if (ko.Result is not null) { continue; }
			if (IsNonPool(ko.HomeQual))
			{
				var resolved = QualifierResolver.TryResolve(c, ko.HomeQual);
				if (resolved is not null) { ko.HomeTeamId = resolved; }
			}
			if (IsNonPool(ko.AwayQual))
			{
				var resolved = QualifierResolver.TryResolve(c, ko.AwayQual);
				if (resolved is not null) { ko.AwayTeamId = resolved; }
			}
		}
		ResolveThirdPlacePools(c);
	}

	static bool IsNonPool(string dsl) =>
		QualifierParser.TryParse(dsl, out var q) && q is not ThirdPlacePool;

	/// <summary>
	/// Batch-assign 3rd-place pool slots. Globally ranks 3rd-placers across all groups
	/// referenced by any pool slot, then matches teams to slots in rank order via
	/// augmenting paths (Kuhn's algorithm): a team whose eligible slots are all taken
	/// may relocate an earlier qualifier to one of its alternative slots. Guarantees
	/// every slot fills whenever a valid assignment exists, and that the qualifiers
	/// are exactly the best-ranked assignable teams — first-fit can strand a slot
	/// whose eligible teams were all diverted into earlier slots.
	///
	/// No-op until every referenced group completes, then fills all pool slots in one
	/// pass; subsequent calls collect no uninitialized pool slots and return
	/// immediately, keeping the post-sim refresh path cheap on every game tick.
	/// </summary>
	static void ResolveThirdPlacePools(Competition c)
	{
		var poolSlots = new List<(KoGame Game, bool IsHome, ThirdPlacePool Pool)>();
		foreach (var ko in c.Games.OfType<KoGame>())
		{
			if (!ko.IsHomeInitialized
				&& QualifierParser.TryParse(ko.HomeQual, out var qh)
				&& qh is ThirdPlacePool ph)
			{
				poolSlots.Add((ko, true, ph));
			}
			if (!ko.IsAwayInitialized
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

		// slotTeam[s] = index into thirdPlacers occupying slot s, -1 = free.
		var slotTeam = new int[poolSlots.Count];
		Array.Fill(slotTeam, -1);

		// Kuhn's augmenting path: claim a free eligible slot, or recursively relocate its occupant to one of their alternatives.
		bool TryPlace(int teamIdx, bool[] visited)
		{
			for (var s = 0; s < poolSlots.Count; s++)
			{
				if (visited[s] || !poolSlots[s].Pool.EligibleGroups.Contains(thirdPlacers[teamIdx].Letter)) { continue; }
				visited[s] = true;
				if (slotTeam[s] < 0 || TryPlace(slotTeam[s], visited))
				{
					slotTeam[s] = teamIdx;
					return true;
				}
			}
			return false;
		}

		var filled = 0;
		for (var t = 0; t < thirdPlacers.Count && filled < poolSlots.Count; t++)
		{
			if (TryPlace(t, new bool[poolSlots.Count])) { filled++; }
		}

		if (filled < poolSlots.Count)
		{
			throw new InvalidOperationException(
				$"Third-place pools in '{c.DefinitionId}' leave {poolSlots.Count - filled} of {poolSlots.Count} slots unfillable.");
		}

		for (var s = 0; s < poolSlots.Count; s++)
		{
			var (game, isHome, _) = poolSlots[s];
			var teamId = thirdPlacers[slotTeam[s]].Standing.TeamId;
			if (isHome) { game.HomeTeamId = teamId; }
			else { game.AwayTeamId = teamId; }
		}
	}
}
