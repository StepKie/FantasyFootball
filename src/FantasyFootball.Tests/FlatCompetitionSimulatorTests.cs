namespace FantasyFootball.Tests;

/// <summary>
/// End-to-end simulator test: every flavor of game gets a result, KO
/// qualifier chain fills in, simulation timestamps are stamped.
/// </summary>
public class FlatCompetitionSimulatorTests
{
	readonly EmbeddedCompetitionDefinitionStore _definitions = new();
	readonly FlatCompetitionSimulator _simulator = new(new StubScoreModel());

	[Fact]
	public void Simulate_Wm2022_AllGamesGetResults()
	{
		var c = _definitions.Load("wm-2022");
		_simulator.Simulate(c);

		c.Games.Should().OnlyContain(g => g.Result != null);
		c.IsFinished().Should().BeTrue();
	}

	[Fact]
	public void Simulate_Wm2022_KoGames_HaveResolvedTeamIds()
	{
		var c = _definitions.Load("wm-2022");
		_simulator.Simulate(c);

		var koGames = c.Games.OfType<FlatKoGame>().ToList();
		koGames.Should().OnlyContain(g => g.HomeTeamId != null && g.AwayTeamId != null);
	}

	[Fact]
	public void Simulate_Wm2022_KoGames_AreDecisive()
	{
		var c = _definitions.Load("wm-2022");
		_simulator.Simulate(c);

		var koGames = c.Games.OfType<FlatKoGame>().ToList();
		koGames.Should().OnlyContain(g => g.Result!.Value.HomeScore != g.Result.Value.AwayScore,
			"every KO game must have a winner — IScoreModel.ScoreKoGame contract");
	}

	[Fact]
	public void Simulate_StampsSimulationTimestamps()
	{
		var c = _definitions.Load("wm-2022");
		var before = DateTime.UtcNow;

		_simulator.Simulate(c);

		c.SimulationStart.Should().BeOnOrAfter(before);
		c.SimulationFinished.Should().BeOnOrAfter(c.SimulationStart!.Value);
	}

	[Fact]
	public void Simulate_Wm2022_ProducesWinner()
	{
		var c = _definitions.Load("wm-2022");
		_simulator.Simulate(c);

		c.WinnerTeamId().Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void Simulate_AlreadyPlayedGames_NotRescored()
	{
		var c = _definitions.Load("wm-2022");
		// Pre-set game 1's result; the simulator should leave it alone.
		var preset = new FlatResult(7, 7, GameEnd.NORMAL);     // intentionally weird so we can detect rewrite
		var game1 = (FlatGroupGame)c.Games.First(g => g.Id == 1);
		game1.Result = preset;

		_simulator.Simulate(c);

		game1.Result.Should().Be(preset, "simulator should skip games that already have a Result");
	}

	[Fact]
	public void Simulate_Em2024_AllGamesGetResults_NoThirdPlaceGame()
	{
		// EM format has no third-place game, but otherwise should round-trip cleanly.
		var c = _definitions.Load("em-2024");
		_simulator.Simulate(c);
		c.IsFinished().Should().BeTrue();
		c.Rounds.Select(r => r.Id).Should().NotContain("third");
	}

	[Fact]
	public void Simulate_Wm2026_AllGamesGetResults_ExercisesThirdPlacePool()
	{
		// WC48 is the format that uses third-place pool qualifiers in R32.
		// Successful end-to-end sim of this format proves the pool path
		// actually runs (vs. throwing) and resolves to real team IDs.
		var c = _definitions.Load("wm-2026");
		_simulator.Simulate(c);

		c.IsFinished().Should().BeTrue();
		var koGames = c.Games.OfType<FlatKoGame>().ToList();
		koGames.Should().HaveCount(32, "WC48 has R32+R16+QF+SF+3rd+Final = 16+8+4+2+1+1");
		koGames.Should().OnlyContain(g => g.HomeTeamId != null && g.AwayTeamId != null);
	}

	[Fact]
	public void Simulate_AnyFormat_NoTeamFillsTwoSlotsInSameRound()
	{
		// Asserts each team appears at most once per round — uniqueness, not just slot population.
		foreach (var defId in new[] { "wm-2022", "em-2024", "wm-2026" })
		{
			var c = _definitions.Load(defId);
			_simulator.Simulate(c);

			foreach (var round in c.Rounds)
			{
				var teamsInRound = c.RoundGames(round.Id)
					.OfType<FlatKoGame>()
					.SelectMany(g => new[] { g.HomeTeamId!, g.AwayTeamId! })
					.ToList();
				teamsInRound.Should().OnlyHaveUniqueItems(
					$"{defId} round '{round.Id}' must have each team in at most one slot");
			}
		}
	}
}
