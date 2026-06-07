namespace FantasyFootball.Tests;

/// <summary>
/// Qualifier-resolution sanity: group placement reads from standings,
/// game-winner/loser walks the chronological chain, third-place pools
/// refuse per-slot resolution (they resolve only as a batch in
/// CompetitionSimulator). Drives off the embedded definitions plus
/// scripted results — no full sim needed.
/// </summary>
/// <remarks>
/// TryResolve semantics pinned by the tests below: GameWinner / GameLoser
/// and ThirdPlacePool fail-closed (return null when the referenced game is
/// unplayed / always, respectively); GroupPlacement fails open (returns the
/// alphabetical-default team because <c>Standings()</c> doesn't gate on
/// completion). The fail-open path is a known limitation — it lets
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
		// wm-2026 is a future tournament with no baked results.
		var c = _definitions.Load("wm-2026");
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
	public void Resolve_ThirdPlacePool_Throws_PoolsAreBatchOnly()
	{
		var c = _definitions.Load("wm-2026");
		Action act = () => QualifierResolver.Resolve(c, "A/B3");
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*third-place pool*");
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
		var c = _definitions.Load("wm-2026");
		QualifierResolver.TryResolve(c, "W-1").Should().BeNull();
	}

	[Fact]
	public void TryResolve_GameLoser_UnplayedGame_ReturnsNull()
	{
		var c = _definitions.Load("wm-2026");
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
	public void TryResolve_ThirdPlacePool_ReturnsNull()
	{
		var c = _definitions.Load("wm-2022");
		QualifierResolver.TryResolve(c, "A/B3").Should().BeNull();
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
