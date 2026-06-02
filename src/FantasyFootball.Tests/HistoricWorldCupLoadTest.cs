using FantasyFootball.Models;

namespace FantasyFootball.Tests;

/// <summary>
/// Regression guard for the historic World Cups: each loads via
/// HistoricalSpec as a finished competition with the real champion +
/// expected game count, and re-simulates cleanly through its KO bracket.
/// </summary>
public class HistoricWorldCupLoadTest
{
	readonly EmbeddedCompetitionDefinitionStore _definitions = new();
	readonly CompetitionFactory _factory;

	public HistoricWorldCupLoadTest() => _factory = new(_definitions);

	[Theory]
	[InlineData("wm-1962", 32, "BRA")]
	[InlineData("wm-1966", 32, "ENG")]
	[InlineData("wm-1970", 32, "BRA")]
	[InlineData("wm-1986", 52, "ARG")]
	[InlineData("wm-1990", 52, "FRG")]
	[InlineData("wm-1994", 52, "BRA")]
	[InlineData("wm-1998", 64, "FRA")]
	[InlineData("wm-2002", 64, "BRA")]
	[InlineData("wm-2006", 64, "ITA")]
	[InlineData("wm-2010", 64, "ESP")]
	[InlineData("wm-2014", 64, "GER")]
	[InlineData("wm-2018", 64, "FRA")]
	[InlineData("wm-2022", 64, "ARG")]
	public void HistoricWorldCup_LoadsFinishedWithRealChampion(string definitionId, int gameCount, string champion)
	{
		var c = _factory.Create(new HistoricalSpec { DefinitionId = definitionId });

		c.Games.Should().HaveCount(gameCount);
		c.Games.Should().OnlyContain(g => g.Result != null);
		c.IsFinished().Should().BeTrue();
		c.WinnerTeamId().Should().Be(champion);
	}

	[Fact]
	public void WorldCup1970_ReSim_RunsThroughKnockoutBracket()
	{
		var c = _factory.Create(new HistoricalSpec { DefinitionId = "wm-1970", Played = false });
		c.IsFinished().Should().BeFalse();

		new CompetitionSimulator().Simulate(c, new StubScoreModel());

		c.IsFinished().Should().BeTrue();
		c.WinnerTeamId().Should().NotBeNull();
	}

	[Fact]
	public void WorldCup2018_ReSim_RunsThroughR16Bracket()
	{
		var c = _factory.Create(new HistoricalSpec { DefinitionId = "wm-2018", Played = false });
		new CompetitionSimulator().Simulate(c, new StubScoreModel());

		c.IsFinished().Should().BeTrue();
		c.WinnerTeamId().Should().NotBeNull();
	}

	[Fact]
	public void WorldCup1990_ReSim_ResolvesBestThirdPoolsWithoutDuplicates()
	{
		// 24-team: four best-3rd R16 slots share the A/B/C/D/E/F3 pool.
		var c = _factory.Create(new HistoricalSpec { DefinitionId = "wm-1990", Played = false });
		new CompetitionSimulator().Simulate(c, new StubScoreModel());

		c.IsFinished().Should().BeTrue();
		c.WinnerTeamId().Should().NotBeNull();
	}

	[Fact]
	public void WorldCup2002_ReSim_RunsThroughNonStandardR16Bracket()
	{
		// 2002's R16 cross-bracket is non-standard (1E-2B, 1A-2F, ...).
		var c = _factory.Create(new HistoricalSpec { DefinitionId = "wm-2002", Played = false });
		new CompetitionSimulator().Simulate(c, new StubScoreModel());

		c.IsFinished().Should().BeTrue();
		c.WinnerTeamId().Should().NotBeNull();
	}
}
