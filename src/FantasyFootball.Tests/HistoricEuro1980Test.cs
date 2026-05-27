using FantasyFootball.Models;

namespace FantasyFootball.Tests;

/// <summary>
/// Validates the group-based historic-competition mechanics on Euro 1980:
/// baked-in results browse as the real outcome (HistoricalSpec), while the
/// replay path (CustomLineupSpec) resets to a scheduled competition.
/// </summary>
public class HistoricEuro1980Test
{
	readonly EmbeddedCompetitionDefinitionStore _definitions = new();
	readonly CompetitionFactory _factory;

	public HistoricEuro1980Test() => _factory = new(_definitions);

	[Fact]
	public void Euro1980_Historical_LoadsFinishedWithRealChampion()
	{
		var c = _factory.Create(new HistoricalSpec { DefinitionId = "em-1980" });

		c.Games.Should().HaveCount(14);
		c.Games.Should().OnlyContain(g => g.Result != null);
		c.IsFinished().Should().BeTrue();
		c.WinnerTeamId().Should().Be("FRG");

		var thirdPlace = c.Games.Single(g => g.RoundId == "third");
		thirdPlace.Result!.Value.Ending.Should().Be(GameEnd.PENALTIES);
		thirdPlace.Result!.Value.PenaltyHome.Should().Be(9);
	}

	[Fact]
	public void Euro1980_Replay_ResetsToScheduled()
	{
		// The replay path clones the same lineup via CustomLineupSpec — which must clear the baked results so the user re-simulates from scratch.
		var lineup = new[]
		{
			new[] { "FRG", "NED", "TCH", "GRE" },
			new[] { "ITA", "ENG", "ESP", "BEL" },
		};
		var c = _factory.Create(new CustomLineupSpec { DefinitionId = "em-1980", Groups = lineup });

		c.Games.Should().OnlyContain(g => g.Result == null);
		c.IsFinished().Should().BeFalse();
		c.SimulationFinished.Should().BeNull();
		c.Games.OfType<KoGame>().Should().OnlyContain(g => g.HomeTeamId == null && g.AwayTeamId == null);
	}
}
