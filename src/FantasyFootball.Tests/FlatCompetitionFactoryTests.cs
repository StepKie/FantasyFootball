namespace FantasyFootball.Tests;

/// <summary>
/// Tests the spec → FlatCompetition mapping for all three bulk-sim
/// modes (Historical, CustomLineup, RandomLineup). Confirms that the
/// chosen lineup actually reaches both <c>GroupAssignments</c> and
/// each group game's <c>HomeTeamId</c> / <c>AwayTeamId</c>, and that
/// the within-group home/away pairing of the definition is preserved
/// under substitution.
/// </summary>
public class FlatCompetitionFactoryTests
{
	readonly EmbeddedCompetitionDefinitionStore _definitions = new();
	readonly FlatCompetitionFactory _factory;

	public FlatCompetitionFactoryTests()
	{
		_factory = new(_definitions);
	}

	[Fact]
	public void Historical_PreservesDefinitionLineup()
	{
		var spec = new HistoricalSpec { DefinitionId = "wm-2022" };
		var c = _factory.Create(spec);

		c.GroupAssignments[0].Should().BeEquivalentTo(["QAT", "ECU", "SEN", "NED"]);
		var firstGame = (FlatGroupGame)c.Games.First(g => g is FlatGroupGame);
		firstGame.HomeTeamId.Should().Be("QAT");
		firstGame.AwayTeamId.Should().Be("ECU");
	}

	[Fact]
	public void Historical_TwoCreations_AreIndependentInstances()
	{
		// The factory must not share mutable state across calls — bulk
		// sim relies on this to run N independent competitions.
		var spec = new HistoricalSpec { DefinitionId = "wm-2022" };
		var a = _factory.Create(spec);
		var b = _factory.Create(spec);

		a.Should().NotBeSameAs(b);
		a.Games.Should().NotBeSameAs(b.Games);
		a.GroupAssignments.Should().NotBeSameAs(b.GroupAssignments);

		// Mutate one and confirm the other is untouched
		((FlatGroupGame)a.Games[0]).Result = new FlatResult(3, 0, GameEnd.NORMAL);
		((FlatGroupGame)b.Games[0]).Result.Should().BeNull();
	}

	[Fact]
	public void CustomLineup_ReplacesGroupAssignmentsAndGameTeams()
	{
		// Substitute Group A's [QAT, ECU, SEN, NED] for [BRA, ARG, MEX, USA].
		// All other groups stay as-is.
		var historical = _definitions.Load("wm-2022");
		var custom = (string[][])historical.GroupAssignments.Select(g => (string[])g.Clone()).ToArray();
		custom[0] = ["BRA", "ARG", "MEX", "USA"];

		var spec = new CustomLineupSpec { DefinitionId = "wm-2022", Groups = custom };
		var c = _factory.Create(spec);

		c.GroupAssignments[0].Should().BeEquivalentTo(["BRA", "ARG", "MEX", "USA"]);
		c.GroupAssignments[1].Should().BeEquivalentTo(historical.GroupAssignments[1]);

		// First game in the definition is QAT(pos0) vs ECU(pos1); after
		// substitution that's BRA(pos0) vs ARG(pos1) — the home/away
		// positional pairing is preserved.
		var firstGroupAGame = c.Games.OfType<FlatGroupGame>().First(g => g.GroupLetter == "A");
		firstGroupAGame.HomeTeamId.Should().Be("BRA");
		firstGroupAGame.AwayTeamId.Should().Be("ARG");
	}

	[Fact]
	public void CustomLineup_RejectsWrongGroupCount()
	{
		var bad = new string[3][] { ["A", "B", "C", "D"], ["E", "F", "G", "H"], ["I", "J", "K", "L"] };
		var spec = new CustomLineupSpec { DefinitionId = "wm-2022", Groups = bad };
		Action act = () => _factory.Create(spec);
		act.Should().Throw<ArgumentException>().WithMessage("*3 groups*8*");
	}

	[Fact]
	public void CustomLineup_RejectsWrongTeamsPerGroup()
	{
		var bad = new string[8][];
		for (int i = 0; i < 8; i++) { bad[i] = new[] { $"T{i}A", $"T{i}B" }; }    // 2 per group, need 4
		var spec = new CustomLineupSpec { DefinitionId = "wm-2022", Groups = bad };
		Action act = () => _factory.Create(spec);
		act.Should().Throw<ArgumentException>().WithMessage("*2 teams*4 per group*");
	}

	[Fact]
	public void RandomLineup_CallsDrawAlgorithmWithFormatDimensions()
	{
		var draw = new RecordingDraw();
		var spec = new RandomLineupSpec { DefinitionId = "wm-2026", DrawAlgorithm = draw };
		_ = _factory.Create(spec);

		// WC2026: 12 groups × 4 teams.
		draw.LastGroupCount.Should().Be(12);
		draw.LastTeamsPerGroup.Should().Be(4);
	}

	[Fact]
	public void RandomLineup_AppliesDrawnTeamsToGroupGames()
	{
		// Draw algorithm hands back a deterministic synthetic lineup so we
		// can verify the factory wires it through to the game rows.
		var draw = new SyntheticDraw();
		var spec = new RandomLineupSpec { DefinitionId = "wm-2022", DrawAlgorithm = draw };
		var c = _factory.Create(spec);

		c.GroupAssignments[0].Should().BeEquivalentTo(["A1", "A2", "A3", "A4"]);
		c.GroupAssignments[7].Should().BeEquivalentTo(["H1", "H2", "H3", "H4"]);
		var firstGroupA = c.Games.OfType<FlatGroupGame>().First(g => g.GroupLetter == "A");
		firstGroupA.HomeTeamId.Should().StartWith("A");
		firstGroupA.AwayTeamId.Should().StartWith("A");
	}

	sealed class RecordingDraw : IDrawAlgorithm
	{
		public int LastGroupCount { get; private set; }
		public int LastTeamsPerGroup { get; private set; }

		public string[][] Draw(int groupCount, int teamsPerGroup)
		{
			LastGroupCount = groupCount;
			LastTeamsPerGroup = teamsPerGroup;
			var result = new string[groupCount][];
			for (int g = 0; g < groupCount; g++)
			{
				result[g] = new string[teamsPerGroup];
				for (int t = 0; t < teamsPerGroup; t++)
				{
					result[g][t] = $"{(char)('A' + g)}{t + 1}";
				}
			}
			return result;
		}
	}

	sealed class SyntheticDraw : IDrawAlgorithm
	{
		public string[][] Draw(int groupCount, int teamsPerGroup)
		{
			var result = new string[groupCount][];
			for (int g = 0; g < groupCount; g++)
			{
				result[g] = new string[teamsPerGroup];
				for (int t = 0; t < teamsPerGroup; t++)
				{
					result[g][t] = $"{(char)('A' + g)}{t + 1}";
				}
			}
			return result;
		}
	}
}
