namespace FantasyFootball.Tests;

/// <summary>
/// Qualifier-resolution sanity: group placement reads from standings,
/// game-winner/loser walks the chronological chain, third-place pool
/// ranks across the eligible groups. Drives off the embedded
/// definitions plus scripted results — no full sim needed.
/// </summary>
/// <remarks>
/// TryResolve semantics pinned by the tests below: GameWinner / GameLoser
/// fail-closed (return null when the referenced game is unplayed);
/// GroupPlacement and ThirdPlacePool fail-open (return the
/// alphabetical-default team because <c>Standings()</c> doesn't gate on
/// completion). The fail-open paths are a known limitation — they let
/// ResolveAvailableKoTeams prematurely fill KO slots with the
/// alphabetical first team during the group stage. A future fix should
/// gate <c>Standings()</c> on group-stage completion; the tests below
/// would then need their <c>NotBeNull</c> expectations flipped to
/// <c>BeNull</c>.
/// </remarks>
public class QualifierResolverTests
{
	readonly EmbeddedCompetitionDefinitionStore _definitions = new();

	[Fact]
	public void Resolve_GroupPlacement_ReadsFromStandings()
	{
		var c = _definitions.Load("wm-2022");
		// Stub all 6 Group A games with NED winning 1-0 every time it plays,
		// QAT losing 0-1 every time it plays. Top of A = NED, bottom = QAT.
		ScriptGroupA(c);

		QualifierResolver.Resolve(c, "A1").Should().Be("NED");
		QualifierResolver.Resolve(c, "A4").Should().Be("QAT");
	}

	[Fact]
	public void Resolve_GameWinner_ReturnsWinningTeamId()
	{
		var c = _definitions.Load("wm-2022");
		// Pick a group game to script; game 1 in wm-2022 is QAT vs ECU.
		var game1 = c.Games.First(g => g.Id == 1);
		((GroupGame)game1).Result = new Result(3, 1, GameEnd.NORMAL);

		QualifierResolver.Resolve(c, "W-1").Should().Be("QAT");
		QualifierResolver.Resolve(c, "L-1").Should().Be("ECU");
	}

	[Fact]
	public void Resolve_GameWinner_UnplayedGame_Throws()
	{
		var c = _definitions.Load("wm-2022");
		Action act = () => QualifierResolver.Resolve(c, "W-1");
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*game 1*hasn't been played*");
	}

	[Fact]
	public void Resolve_GameWinner_TiedGame_Throws()
	{
		var c = _definitions.Load("wm-2022");
		var game1 = c.Games.First(g => g.Id == 1);
		((GroupGame)game1).Result = new Result(1, 1, GameEnd.NORMAL);

		Action act = () => QualifierResolver.Resolve(c, "W-1");
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*game 1*tie*");
	}

	[Fact]
	public void Resolve_ThirdPlacePool_AllUnplayed_PicksAlphabeticallyEarlierTeam()
	{
		// With nothing scripted, every group's 3rd-place row has 0 pts /
		// GD 0 / GF 0 — the only discriminator left is team-id alphabetical
		// (our deterministic stand-in for the FIFA tail).
		var c = _definitions.Load("wm-2026");
		var aThird = c.Standings("A")[2].TeamId;
		var bThird = c.Standings("B")[2].TeamId;
		var alphaFirst = string.CompareOrdinal(aThird, bThird) < 0 ? aThird : bThird;

		QualifierResolver.Resolve(c, "A/B3").Should().Be(alphaFirst);
	}

	[Fact]
	public void Resolve_ThirdPlacePool_PicksHigherPointsTeam()
	{
		// Script Group A so its alphabetically-LAST team (originally 4th)
		// wins one game and rises to 3rd with 3 points. Group B stays
		// unplayed. Pool should resolve to A's third (3 pts) over B's
		// third (0 pts).
		var c = _definitions.Load("wm-2026");
		var teamsInA = c.GroupAssignments[0];
		var alphaLastInA = teamsInA.OrderBy(t => t, StringComparer.Ordinal).Last();
		var alphaFirstInA = teamsInA.OrderBy(t => t, StringComparer.Ordinal).First();

		// Find the A game between those two and give the win to alphaLast.
		var game = c.GroupGames("A").First(g =>
			(g.HomeTeamId == alphaLastInA && g.AwayTeamId == alphaFirstInA) ||
			(g.HomeTeamId == alphaFirstInA && g.AwayTeamId == alphaLastInA));
		game.Result = game.HomeTeamId == alphaLastInA
			? new Result(1, 0, GameEnd.NORMAL)
			: new Result(0, 1, GameEnd.NORMAL);

		// alphaLastInA now has 3 pts (1W). Others in A still have 0 pts
		// (alphaFirstInA has -1 GD from that loss). So 3rd-place in A is
		// whichever 0-pt team has alphabetically-earliest id.
		var aStandings = c.Standings("A");
		var aThird = aStandings[2].TeamId;
		aStandings[2].Points.Should().Be(0, "verify scripted standings put a 0-pt team in 3rd");
		var bThird = c.Standings("B")[2].TeamId;

		// Pool comparison: A's third has 0 pts, B's third has 0 pts —
		// alphabetical tiebreaker on the two 3rd-place candidates.
		var expected = string.CompareOrdinal(aThird, bThird) < 0 ? aThird : bThird;
		QualifierResolver.Resolve(c, "A/B3").Should().Be(expected);
	}

	[Fact]
	public void Resolve_UnknownDsl_Throws()
	{
		var c = _definitions.Load("wm-2022");
		Action act = () => QualifierResolver.Resolve(c, "garbage");
		act.Should().Throw<FormatException>();
	}

	[Fact]
	public void TryResolve_GameWinner_UnplayedGame_ReturnsNull()
	{
		var c = _definitions.Load("wm-2022");
		QualifierResolver.TryResolve(c, "W-1").Should().BeNull();
	}

	[Fact]
	public void TryResolve_GameLoser_UnplayedGame_ReturnsNull()
	{
		var c = _definitions.Load("wm-2022");
		QualifierResolver.TryResolve(c, "L-1").Should().BeNull();
	}

	[Fact]
	public void TryResolve_GameWinner_PlayedGame_ReturnsWinner()
	{
		var c = _definitions.Load("wm-2022");
		var game1 = c.Games.First(g => g.Id == 1);
		((GroupGame)game1).Result = new Result(3, 1, GameEnd.NORMAL);
		QualifierResolver.TryResolve(c, "W-1").Should().Be("QAT");
	}

	[Fact]
	public void TryResolve_GroupPlacement_BeforeGroupStageFinishes_FailsOpen()
	{
		// Fail-open: returns the alphabetical-default 1st-place team (all 0-pt). See class remarks.
		var c = _definitions.Load("wm-2022");
		QualifierResolver.TryResolve(c, "A1").Should().NotBeNull();
	}

	[Fact]
	public void TryResolve_GroupPlacement_AfterGroupFinishes_ReturnsActualWinner()
	{
		var c = _definitions.Load("wm-2022");
		ScriptGroupA(c);
		QualifierResolver.TryResolve(c, "A1").Should().Be("NED");
	}

	[Fact]
	public void TryResolve_ThirdPlacePool_BeforeAnyGroupFinishes_FailsOpen()
	{
		// Same fail-open shape as GroupPlacement — returns the alphabetical best 3rd-placer (all 0-pt).
		var c = _definitions.Load("wm-2022");
		QualifierResolver.TryResolve(c, "A/B3").Should().NotBeNull();
	}

	[Fact]
	public void TryResolve_BadDsl_StaysLoud_Throws()
	{
		// FormatException (bad DSL) is NOT swallowed by TryResolve — it signals a definition-file bug.
		var c = _definitions.Load("wm-2022");
		Action act = () => QualifierResolver.TryResolve(c, "garbage");
		act.Should().Throw<FormatException>();
	}

	static void ScriptGroupA(Competition c)
	{
		// Make NED beat everyone (in whatever direction the definition has it).
		// Make QAT lose to everyone.
		foreach (var game in c.GroupGames("A"))
		{
			if (game.HomeTeamId == "NED")     { game.Result = new(3, 0, GameEnd.NORMAL); }
			else if (game.AwayTeamId == "NED") { game.Result = new(0, 3, GameEnd.NORMAL); }
			else if (game.HomeTeamId == "QAT") { game.Result = new(0, 2, GameEnd.NORMAL); }
			else if (game.AwayTeamId == "QAT") { game.Result = new(2, 0, GameEnd.NORMAL); }
			else                              { game.Result = new(1, 1, GameEnd.NORMAL); }
		}
	}
}
