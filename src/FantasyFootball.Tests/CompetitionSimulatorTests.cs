namespace FantasyFootball.Tests;

/// <summary>
/// End-to-end simulator test: every flavor of game gets a result, KO
/// qualifier chain fills in, simulation timestamps are stamped.
/// </summary>
public class CompetitionSimulatorTests
{
	readonly EmbeddedCompetitionDefinitionStore _definitions = new();
	readonly CompetitionSimulator _simulator = new();
	readonly StubScoreModel _scoreModel = new();

	[Fact]
	public void Simulate_Wm2022_AllGamesGetResults()
	{
		var c = _definitions.Load("wm-2022");
		_simulator.Simulate(c, _scoreModel);

		c.Games.Should().OnlyContain(g => g.Result != null);
		c.IsFinished().Should().BeTrue();
	}

	[Fact]
	public void Simulate_Wm2022_KoGames_HaveResolvedTeamIds()
	{
		var c = _definitions.Load("wm-2022");
		_simulator.Simulate(c, _scoreModel);

		var koGames = c.Games.OfType<KoGame>().ToList();
		koGames.Should().OnlyContain(g => g.IsFullyInitialized);
	}

	[Fact]
	public void Simulate_Wm2022_KoGames_AreDecisive()
	{
		var c = _definitions.Load("wm-2022");
		_simulator.Simulate(c, _scoreModel);

		var koGames = c.Games.OfType<KoGame>().ToList();
		koGames.Should().OnlyContain(g => g.Result!.Value.HomeWon || g.Result.Value.AwayWon,
			"every KO game must have a winner — IScoreModel.ScoreKoGame contract");
	}

	[Fact]
	public void Simulate_StampsSimulationTimestamps()
	{
		var c = _definitions.Load("wm-2022");
		var before = DateTime.UtcNow;

		_simulator.Simulate(c, _scoreModel);

		c.SimulationStart.Should().BeOnOrAfter(before);
		c.SimulationFinished.Should().BeOnOrAfter(c.SimulationStart!.Value);
	}

	[Fact]
	public void Simulate_Wm2022_ProducesWinner()
	{
		var c = _definitions.Load("wm-2022");
		_simulator.Simulate(c, _scoreModel);

		c.WinnerTeamId().Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void Simulate_AlreadyPlayedGames_NotRescored()
	{
		var c = _definitions.Load("wm-2022");
		// Pre-set game 1's result; the simulator should leave it alone.
		var preset = new Result(7, 7, GameEnd.NORMAL);     // intentionally weird so we can detect rewrite
		var game1 = (GroupGame)c.Games.First(g => g.Id == 1);
		game1.Result = preset;

		_simulator.Simulate(c, _scoreModel);

		game1.Result.Should().Be(preset, "simulator should skip games that already have a Result");
	}

	[Fact]
	public void Simulate_Em2024_AllGamesGetResults_NoThirdPlaceGame()
	{
		// EM format has no third-place game, but otherwise should round-trip cleanly.
		var c = _definitions.Load("em-2024");
		_simulator.Simulate(c, _scoreModel);
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
		_simulator.Simulate(c, _scoreModel);

		c.IsFinished().Should().BeTrue();
		var koGames = c.Games.OfType<KoGame>().ToList();
		koGames.Should().HaveCount(32, "WC48 has R32+R16+QF+SF+3rd+Final = 16+8+4+2+1+1");
		koGames.Should().OnlyContain(g => g.IsFullyInitialized);
	}

	[Fact]
	public void Simulate_AnyFormat_NoTeamFillsTwoSlotsInSameRound()
	{
		// Asserts each team appears at most once per round — uniqueness, not just slot population.
		foreach (var defId in new[] { "wm-2022", "em-2024", "wm-2026" })
		{
			var c = _definitions.Load(defId);
			_simulator.Simulate(c, _scoreModel);

			foreach (var round in c.Rounds)
			{
				var teamsInRound = c.RoundGames(round.Id)
					.OfType<KoGame>()
					.SelectMany(g => new[] { g.HomeTeamId!, g.AwayTeamId! })
					.ToList();
				teamsInRound.Should().OnlyHaveUniqueItems(
					$"{defId} round '{round.Id}' must have each team in at most one slot");
			}
		}
	}

	[Fact]
	public void Wm2026_IncrementalPlay_DoesNotLockKoSlotsWithMidGroupStandings()
	{
		// UI flow: every game triggers RefreshAfterSim → ResolveAvailableKoTeams, which must not lock GroupPlacement slots to partial standings (collides with the pool resolver later).
		var c = _definitions.Load("wm-2026");
		foreach (var game in c.Games.OrderBy(g => g.PlayedOn).ToList())
		{
			_simulator.SimulateGame(c, game, _scoreModel);
			CompetitionSimulator.ResolveAvailableKoTeams(c);
		}

		var r32Teams = c.RoundGames("r32")
			.OfType<KoGame>()
			.SelectMany(g => new[] { g.HomeTeamId, g.AwayTeamId })
			.ToList();
		r32Teams.Should().OnlyHaveUniqueItems("R32 must have each team in at most one slot, even under per-game resolve");
	}

	[Fact]
	public void ResolveAvailableKoTeams_RankingThatStrandsFirstFit_FillsAllPoolSlotsWithTopEight()
	{
		// Ranking B, F, E, I, J, … strands slot B/E/F/I/J3 under first-fit: every eligible team gets diverted into an earlier slot.
		var c = _definitions.Load("wm-2026");
		string[] rankedLetters = ["B", "F", "E", "I", "J", "A", "C", "D", "G", "H", "K", "L"];
		for (var i = 0; i < rankedLetters.Length; i++)
		{
			ScriptGroup(c, rankedLetters[i], thirdPlaceGoals: 14 - i);
		}

		CompetitionSimulator.ResolveAvailableKoTeams(c);

		var poolTeams = new List<string>();
		foreach (var ko in c.Games.OfType<KoGame>())
		{
			if (QualifierParser.TryParse(ko.HomeQual, out var qh) && qh is ThirdPlacePool) { poolTeams.Add(ko.HomeTeamId); }
			if (QualifierParser.TryParse(ko.AwayQual, out var qa) && qa is ThirdPlacePool) { poolTeams.Add(ko.AwayTeamId); }
		}
		var topEight = rankedLetters.Take(8).Select(l => c.Standings(l)[2].TeamId);

		poolTeams.Should().BeEquivalentTo(topEight, "the 8 best-ranked 3rd-placers qualify, each in exactly one R32 slot");
	}

	/// <summary>
	/// Scripts a group deterministically: the alphabetical 1st beats everyone,
	/// 2nd beats 3rd and 4th, 3rd beats 4th by <paramref name="thirdPlaceGoals"/> —
	/// which controls the 3rd-placer's GD/GF and thereby its global pool rank.
	/// </summary>
	static void ScriptGroup(Competition c, string letter, int thirdPlaceGoals)
	{
		var ordered = c.GroupGames(letter)
			.SelectMany(g => new[] { g.HomeTeamId, g.AwayTeamId })
			.Distinct()
			.OrderBy(t => t, StringComparer.Ordinal)
			.ToList();
		foreach (var game in c.GroupGames(letter))
		{
			var home = ordered.IndexOf(game.HomeTeamId);
			var away = ordered.IndexOf(game.AwayTeamId);
			var goals = Math.Min(home, away) == 2 ? thirdPlaceGoals : 1;
			game.Result = home < away ? new Result(goals, 0, GameEnd.NORMAL) : new Result(0, goals, GameEnd.NORMAL);
		}
	}
}
