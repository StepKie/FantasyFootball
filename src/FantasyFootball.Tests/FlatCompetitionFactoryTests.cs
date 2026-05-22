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
		// Synthetic lineup — fully replaces all 8 groups with T01..T32 so
		// the uniqueness invariant is satisfied across the whole lineup.
		// Validates that both GroupAssignments and the group game team IDs
		// get rewired to the new lineup, preserving the definition's
		// positional home/away pairing.
		var custom = new string[8][];
		for (int g = 0; g < 8; g++)
		{
			custom[g] = new string[4];
			for (int t = 0; t < 4; t++)
			{
				custom[g][t] = $"T{g * 4 + t + 1:00}";
			}
		}

		var spec = new CustomLineupSpec { DefinitionId = "wm-2022", Groups = custom };
		var c = _factory.Create(spec);

		c.GroupAssignments[0].Should().BeEquivalentTo(["T01", "T02", "T03", "T04"]);
		c.GroupAssignments[1].Should().BeEquivalentTo(["T05", "T06", "T07", "T08"]);

		// First game in the definition is QAT(pos0) vs ECU(pos1); after
		// substitution that's T01(pos0) vs T02(pos1) — positional pairing preserved.
		var firstGroupAGame = c.Games.OfType<FlatGroupGame>().First(g => g.GroupLetter == "A");
		firstGroupAGame.HomeTeamId.Should().Be("T01");
		firstGroupAGame.AwayTeamId.Should().Be("T02");
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
	public void CustomLineup_RejectsDuplicateTeamIds()
	{
		// Same team in two positions silently corrupts standings (team
		// plays itself, AllTeamIds reports it twice) — uniqueness must be
		// enforced at the validation boundary.
		var dup = new string[8][];
		for (int g = 0; g < 8; g++) { dup[g] = ["T01", "T02", "T03", "T04"]; }     // same 4 teams in every group
		var spec = new CustomLineupSpec { DefinitionId = "wm-2022", Groups = dup };
		Action act = () => _factory.Create(spec);
		act.Should().Throw<ArgumentException>().WithMessage("*duplicate team IDs*");
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
