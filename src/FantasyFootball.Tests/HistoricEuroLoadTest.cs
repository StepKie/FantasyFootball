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
	public void HistoricEuro_LoadsFinishedWithRealChampion(string definitionId, int gameCount, string champion)
	{
		var c = _factory.Create(new HistoricalSpec { DefinitionId = definitionId });

		c.Games.Should().HaveCount(gameCount);
		c.Games.Should().OnlyContain(g => g.Result != null);
		c.IsFinished().Should().BeTrue();
		c.WinnerTeamId().Should().Be(champion);
	}
}
