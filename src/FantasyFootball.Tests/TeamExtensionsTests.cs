namespace FantasyFootball.Tests;

public class TeamExtensionsTests
{
	static Team T(int id, string code3) => new() { Id = id, ShortName = code3 };

	static IActiveEloSet ActiveSetWith(params (string code3, int elo)[] entries)
	{
		var active = new ActiveEloSet();
		active.SetCurrent(new EloSet
		{
			Name = "Test",
			Date = new DateOnly(2026, 1, 1),
			Snapshot = entries.ToDictionary(e => e.code3, e => e.elo),
		});
		return active;
	}

	[Fact]
	public void RankByElo_TopTeam_ReturnsRankOne()
	{
		var roster = new[] { T(1, "AAA"), T(2, "BBB"), T(3, "CCC") };
		var active = ActiveSetWith(("AAA", 1800), ("BBB", 1700), ("CCC", 1600));
		roster.RankByElo(active, 1).Should().Be(1);
	}

	[Fact]
	public void RankByElo_MiddleAndLastTeams_ReturnSortedPositions()
	{
		var roster = new[] { T(1, "AAA"), T(2, "BBB"), T(3, "CCC") };
		var active = ActiveSetWith(("AAA", 1800), ("BBB", 1700), ("CCC", 1600));
		roster.RankByElo(active, 2).Should().Be(2);
		roster.RankByElo(active, 3).Should().Be(3);
	}

	[Fact]
	public void RankByElo_TiedElos_ResolveByRosterInputOrder()
	{
		// BBB and CCC both at 1700; OrderByDescending is stable so input order wins.
		var roster = new[] { T(1, "AAA"), T(2, "BBB"), T(3, "CCC"), T(4, "DDD") };
		var active = ActiveSetWith(("AAA", 1800), ("BBB", 1700), ("CCC", 1700), ("DDD", 1600));
		roster.RankByElo(active, 2).Should().Be(2);
		roster.RankByElo(active, 3).Should().Be(3);
	}

	[Fact]
	public void RankByElo_TeamNotInRoster_ReturnsZeroSentinel()
	{
		var roster = new[] { T(1, "AAA"), T(2, "BBB") };
		var active = ActiveSetWith(("AAA", 1800), ("BBB", 1700));
		roster.RankByElo(active, 99).Should().Be(0);
	}

	[Fact]
	public void RankByElo_EmptyRoster_ReturnsZero()
	{
		var active = ActiveSetWith();
		Array.Empty<Team>().RankByElo(active, 1).Should().Be(0);
	}
}
