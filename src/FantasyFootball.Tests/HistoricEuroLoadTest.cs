using FantasyFootball.Models;

namespace FantasyFootball.Tests;

/// <summary>
/// Regression guard for the group-based historic Euros: each definition
/// loads via HistoricalSpec as a finished competition with the real
/// champion and the expected game count.
/// </summary>
public class HistoricEuroLoadTest
{
	readonly EmbeddedCompetitionDefinitionStore _definitions = new();
	readonly CompetitionFactory _factory;

	public HistoricEuroLoadTest() => _factory = new(_definitions);

	[Theory]
	[InlineData("em-1980", 14, "FRG")]
	[InlineData("em-1984", 15, "FRA")]
	[InlineData("em-1988", 15, "NED")]
	[InlineData("em-1996", 31, "GER")]
	[InlineData("em-2000", 31, "FRA")]
	[InlineData("em-2004", 31, "GRE")]
	public void HistoricEuro_LoadsFinishedWithRealChampion(string definitionId, int gameCount, string champion)
	{
		var c = _factory.Create(new HistoricalSpec { DefinitionId = definitionId });

		c.Games.Should().HaveCount(gameCount);
		c.Games.Should().OnlyContain(g => g.Result != null);
		c.IsFinished().Should().BeTrue();
		c.WinnerTeamId().Should().Be(champion);
	}

	[Fact]
	public void Euro1996_ReSim_RunsThroughKoBracketFromScratch()
	{
		// Played=false loads the real lineup but scheduled — no Groups needed.
		// Simulating exercises the full QF→SF→Final qualifier chain (A1/B2 +
		// W-NN); a broken bracket wire (bad W-NN id) would throw here.
		var c = _factory.Create(new HistoricalSpec { DefinitionId = "em-1996", Played = false });
		c.IsFinished().Should().BeFalse();
		c.Games.OfType<KoGame>().Should().OnlyContain(g => g.HomeTeamId == null && g.AwayTeamId == null);

		new CompetitionSimulator(new StubScoreModel()).Simulate(c);

		c.IsFinished().Should().BeTrue();
		c.WinnerTeamId().Should().NotBeNull();
	}
}
