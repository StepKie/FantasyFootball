namespace FantasyFootball.Models;

/// <summary>
/// Projections over the flat Competition shape. Replaces the
/// parent-child navigation (game.Round.Stage…) the old graph model
/// supported.
///
/// </summary>
public static class CompetitionExtensions
{
	/// <summary>
	/// All participant team IDs across all groups (flat union). The order
	/// follows group letter order: GroupAssignments[0]'s teams first, then
	/// GroupAssignments[1]'s, etc.
	/// </summary>
	public static IEnumerable<string> AllTeamIds(this Competition c) =>
		c.GroupAssignments.SelectMany(g => g);

	/// <summary>All games in a given group letter (e.g. "A"). Empty for KO games.</summary>
	public static IEnumerable<GroupGame> GroupGames(this Competition c, string letter) =>
		c.Games.OfType<GroupGame>().Where(g => g.GroupLetter == letter);

	/// <summary>All games in a given round ID.</summary>
	public static IEnumerable<Game> RoundGames(this Competition c, string roundId) =>
		c.Games.Where(g => g.RoundId == roundId);

	/// <summary>All games in a given stage ID (resolved via Round.StageId).</summary>
	public static IEnumerable<Game> StageGames(this Competition c, string stageId)
	{
		var roundIds = c.Rounds.Where(r => r.StageId == stageId).Select(r => r.Id).ToHashSet();
		return c.Games.Where(g => roundIds.Contains(g.RoundId));
	}

	/// <summary>
	/// All games siblings of <paramref name="g"/> in the same round (excluding itself).
	/// </summary>
	public static IEnumerable<Game> SameRoundGames(this Competition c, Game g) =>
		c.Games.Where(x => x.RoundId == g.RoundId && x.Id != g.Id);

	/// <summary>
	/// All games siblings of <paramref name="g"/> in the same group (excluding itself).
	/// Empty if <paramref name="g"/> is not a group-stage game.
	/// </summary>
	public static IEnumerable<GroupGame> SameGroupGames(this Competition c, Game g) =>
		g is GroupGame gg
			? c.GroupGames(gg.GroupLetter).Where(x => x.Id != g.Id)
			: Enumerable.Empty<GroupGame>();

	/// <summary>
	/// Latest finished game by PlayedOn, or null if none played yet.
	/// Id is the tiebreaker for same-timestamp games (every MD3 group ties)
	/// so Undo/Reroll on simultaneous games hits the JSON-order-latest one.
	/// </summary>
	public static Game? LastFinishedGame(this Competition c) =>
		c.Games.Where(g => g.Result is not null).MaxBy(g => (g.PlayedOn, g.Id));

	/// <summary>First scheduled (not yet played) game by PlayedOn, or null if competition is finished.</summary>
	public static Game? CurrentGame(this Competition c) =>
		c.Games.Where(g => g.Result is null).MinBy(g => g.PlayedOn);

	/// <summary>True if every game has a Result.</summary>
	public static bool IsFinished(this Competition c) =>
		c.Games.All(g => g.Result is not null);

	/// <summary>
	/// Team ID of the competition winner — winner of the final game. Null
	/// if no game has a Result yet or the final hasn't been played.
	/// </summary>
	public static string? WinnerTeamId(this Competition c)
	{
		if (c.Rounds.Length == 0) { return null; }
		// The "final" is the game in the round with the highest Order that has a Result.
		var maxOrder = c.Rounds.Max(r => r.Order);
		var finalRoundIds = c.Rounds.Where(r => r.Order == maxOrder).Select(r => r.Id).ToHashSet();
		var finalGame = c.Games.LastOrDefault(g => finalRoundIds.Contains(g.RoundId) && g.Result is not null);
		if (finalGame is null || finalGame.Result is not { } r) { return null; }
		return r.HomeWon ? HomeTeamIdOf(finalGame) : AwayTeamIdOf(finalGame);
	}

	/// <summary>Resolved home team id, or null if a KO qualifier hasn't resolved yet.</summary>
	public static string? HomeTeamIdOf(Game g) => g switch
	{
		GroupGame gg => gg.HomeTeamId,
		KoGame kg => kg.HomeTeamId,
		_ => null,
	};

	/// <summary>Resolved away team id, or null if a KO qualifier hasn't resolved yet.</summary>
	public static string? AwayTeamIdOf(Game g) => g switch
	{
		GroupGame gg => gg.AwayTeamId,
		KoGame kg => kg.AwayTeamId,
		_ => null,
	};

	/// <summary>
	/// 1-based position of <paramref name="game"/> within its round
	/// (chronological by <c>PlayedOn</c>). Used to build labels like
	/// "R16 1" or "Winner of QF #2".
	/// </summary>
	public static int PositionInRound(this Competition c, Game game)
	{
		var match = c.Games
			.Where(g => g.RoundId == game.RoundId)
			.OrderBy(g => (g.PlayedOn, g.Id))
			.Select((g, idx) => (g, idx))
			.FirstOrDefault(x => x.g.Id == game.Id);
		// Sentinel 0 for not-found so a stale game-vs-competition mismatch renders as "QF 0" — visible without crashing.
		return match.g is null ? 0 : match.idx + 1;
	}

	/// <summary>
	/// Standings table for a single group letter ("A", "B", …). Pure
	/// function — no caching; cheap enough at our scale (4 teams ×
	/// 3 games per group) to recompute on demand. Position is 1-based,
	/// FIFA-style tiebreaker cascade: points → goal difference →
	/// goals for → team-id alphabetical (the last as a deterministic
	/// stand-in for head-to-head / fair play / drawing of lots, which
	/// we don't model yet).
	/// </summary>
	public static IReadOnlyList<GroupStanding> Standings(this Competition c, string groupLetter)
	{
		var groupIndex = groupLetter[0] - 'A';
		if (groupIndex < 0 || groupIndex >= c.GroupAssignments.Length)
		{
			throw new ArgumentException(
				$"Group '{groupLetter}' is out of range for competition '{c.DefinitionId}' (has {c.GroupAssignments.Length} groups).",
				nameof(groupLetter));
		}
		var teamIds = c.GroupAssignments[groupIndex];
		var stats = teamIds.ToDictionary(t => t, _ => new MutableRow());

		foreach (var game in c.GroupGames(groupLetter))
		{
			if (game.Result is not { } r) { continue; }     // unplayed — skip
			var home = stats[game.HomeTeamId];
			var away = stats[game.AwayTeamId];
			home.Played++; away.Played++;
			home.GoalsFor += r.HomeScore; home.GoalsAgainst += r.AwayScore;
			away.GoalsFor += r.AwayScore; away.GoalsAgainst += r.HomeScore;
			if (r.HomeWon) { home.Wins++; away.Losses++; }
			else if (r.AwayWon) { away.Wins++; home.Losses++; }
			else { home.Draws++; away.Draws++; }
		}

		var ordered = stats
			.Select(kv => (TeamId: kv.Key, Row: kv.Value))
			.OrderByDescending(x => x.Row.Points)
			.ThenByDescending(x => x.Row.GoalsFor - x.Row.GoalsAgainst)
			.ThenByDescending(x => x.Row.GoalsFor)
			.ThenBy(x => x.TeamId, StringComparer.Ordinal)
			.ToList();

		var result = new GroupStanding[ordered.Count];
		for (int i = 0; i < ordered.Count; i++)
		{
			var (teamId, row) = ordered[i];
			result[i] = new GroupStanding(
				teamId,
				row.Played,
				row.Wins,
				row.Draws,
				row.Losses,
				row.GoalsFor,
				row.GoalsAgainst,
				row.Points,
				Position: i + 1);
		}
		return result;
	}

	sealed class MutableRow
	{
		public int Played;
		public int Wins;
		public int Draws;
		public int Losses;
		public int GoalsFor;
		public int GoalsAgainst;
		public int Points => Wins * 3 + Draws;
	}
}
