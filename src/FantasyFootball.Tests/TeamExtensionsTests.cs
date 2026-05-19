namespace FantasyFootball.Tests;

public class TeamExtensionsTests
{
	static Team T(int id, int elo) => new() { Id = id, Elo = elo };

	[Fact]
	public void RankByElo_TopTeam_ReturnsRankOne()
	{
		var roster = new[] { T(1, 1800), T(2, 1700), T(3, 1600) };
		roster.RankByElo(1).Should().Be(1);
	}

	[Fact]
	public void RankByElo_MiddleAndLastTeams_ReturnSortedPositions()
	{
		var roster = new[] { T(1, 1800), T(2, 1700), T(3, 1600) };
		roster.RankByElo(2).Should().Be(2);
		roster.RankByElo(3).Should().Be(3);
	}

	[Fact]
	public void RankByElo_TiedElos_ResolveByRosterInputOrder()
	{
		// T(2) and T(3) both at 1700; OrderByDescending is stable so input order wins.
		var roster = new[] { T(1, 1800), T(2, 1700), T(3, 1700), T(4, 1600) };
		roster.RankByElo(2).Should().Be(2);
		roster.RankByElo(3).Should().Be(3);
	}

	[Fact]
	public void RankByElo_TeamNotInRoster_ReturnsZeroSentinel()
	{
		var roster = new[] { T(1, 1800), T(2, 1700) };
		roster.RankByElo(99).Should().Be(0);
	}

	[Fact]
	public void RankByElo_EmptyRoster_ReturnsZero()
	{
		Array.Empty<Team>().RankByElo(1).Should().Be(0);
	}
}
