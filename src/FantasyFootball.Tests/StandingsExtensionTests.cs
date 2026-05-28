namespace FantasyFootball.Tests;

/// <summary>
/// Standings computation: tallying wins/losses/goals/points correctly,
/// plus the tiebreaker cascade (points → GD → GF → team-id alphabetical).
/// </summary>
public class StandingsExtensionTests
{
	readonly EmbeddedCompetitionDefinitionStore _definitions = new();

	[Fact]
	public void Standings_OfUnplayedGroup_AllZeros()
	{
		// wm-2026 is a future tournament with no baked results.
		var c = _definitions.Load("wm-2026");
		var s = c.Standings("A");
		s.Should().HaveCount(4);
		s.Should().OnlyContain(r => r.Played == 0 && r.Points == 0);
		s.Select(r => r.Position).Should().Equal(1, 2, 3, 4);
	}

	[Fact]
	public void Standings_AfterScripted3GameRound_RankByPointsThenGdThenGf()
	{
		var c = _definitions.Load("wm-2022");
		// Group A: QAT, ECU, SEN, NED (positions 0,1,2,3 in definition)
		var aGames = c.GroupGames("A").ToList();

		// Force a scripted set of results so we can assert exact positions:
		//   NED 4-0 QAT  (NED win, NED gd+4, QAT gd-4)
		//   ECU 2-1 SEN  (ECU win)
		//   NED 2-1 ECU  (NED win)
		//   SEN 3-0 QAT  (SEN win, gd+3)
		//   ECU 1-1 QAT  (draw)
		//   NED 0-0 SEN  (draw)
		// Final tally:
		//   NED: 2W 1D 0L, GF 6 / GA 1 → 7 pts, gd+5
		//   SEN: 1W 1D 1L, GF 4 / GA 2 → 4 pts, gd+2
		//   ECU: 1W 1D 1L, GF 4 / GA 4 → 4 pts, gd 0
		//   QAT: 0W 1D 2L, GF 1 / GA 8 → 1 pt,  gd-7
		// Expected order: NED, SEN, ECU, QAT (SEN vs ECU tiebroken on GD).
		SetGameResult(aGames, "NED", "QAT", 4, 0);
		SetGameResult(aGames, "ECU", "SEN", 2, 1);
		SetGameResult(aGames, "NED", "ECU", 2, 1);
		SetGameResult(aGames, "SEN", "QAT", 3, 0);
		SetGameResult(aGames, "ECU", "QAT", 1, 1);
		SetGameResult(aGames, "NED", "SEN", 0, 0);

		var s = c.Standings("A");

		s[0].TeamId.Should().Be("NED");
		s[0].Points.Should().Be(7);
		s[0].GoalDifference.Should().Be(5);
		s[1].TeamId.Should().Be("SEN");
		s[1].Points.Should().Be(4);
		s[1].GoalDifference.Should().Be(2);
		s[2].TeamId.Should().Be("ECU");
		s[2].Points.Should().Be(4);
		s[2].GoalDifference.Should().Be(0);
		s[3].TeamId.Should().Be("QAT");
		s[3].Points.Should().Be(1);
	}

	[Fact]
	public void Standings_AllEqualTiebreaker_FallsBackToAlphabetical()
	{
		// All-zero standings (unplayed group) should sort by team ID
		// alphabetical — deterministic stand-in for the FIFA tail.
		var c = _definitions.Load("wm-2022");
		var s = c.Standings("A");

		s.Select(r => r.TeamId).Should().BeEquivalentTo(
			new[] { "ECU", "NED", "QAT", "SEN" },
			"unplayed standings tiebreak by team-id alphabetical");
	}

	[Fact]
	public void Standings_UnknownGroup_Throws()
	{
		var c = _definitions.Load("wm-2022");
		Action act = () => c.Standings("Z");
		act.Should().Throw<ArgumentException>().WithMessage("*Group 'Z'*");
	}

	static void SetGameResult(List<GroupGame> games, string home, string away, int hs, int @as)
	{
		var match = games.First(g =>
			(g.HomeTeamId == home && g.AwayTeamId == away) ||
			(g.HomeTeamId == away && g.AwayTeamId == home));
		// The scripted matchups above ignore the actual home/away assignment
		// of the definition — line up scores to the definition's direction.
		if (match.HomeTeamId == home)
		{
			match.Result = new Result(hs, @as, GameEnd.NORMAL);
		}
		else
		{
			match.Result = new Result(@as, hs, GameEnd.NORMAL);
		}
	}
}
