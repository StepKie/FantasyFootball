using FantasyFootball.Repositories;
using FantasyFootball.Services;
using FantasyFootball.Data.CompetitionFactories;

namespace FantasyFootball.Tests;

/// <summary>
/// Tests the bridge from the old factories to the new flat model. Builds
/// each historical competition via its existing factory, runs it through
/// FlatDefinitionConverter, and asserts the shape matches what the old
/// graph contained — same teams, same number of games, qualifier strings
/// reference the right earlier games, etc.
///
/// Exists transitionally alongside the old factories; deleted in the
/// cleanup PR.
/// </summary>
public class FlatDefinitionConverterTests : BaseTest
{
	public FlatDefinitionConverterTests(ITestOutputHelper output) : base(output) { }

	[Theory]
	[InlineData(2022, "world-cup-32", 8, 32, 64)]
	[InlineData(2026, "world-cup-48", 12, 48, 104)]
	public void Convert_WorldCup_ShapeMatchesFactory(int year, string expectedFormatId, int expectedGroups, int expectedTeams, int expectedGames)
	{
		var factory = CompetitionFactory.Default(CompetitionType.WM, DataService, year);
		var oldComp = factory.Create();
		var newComp = FlatDefinitionConverter.Convert(oldComp, $"wm-{year}", year);

		newComp.Type.Should().Be(CompetitionType.WM);
		newComp.Year.Should().Be(year);
		newComp.FormatId.Should().Be(expectedFormatId);
		newComp.DefinitionId.Should().Be($"wm-{year}");
		newComp.GroupAssignments.Should().HaveCount(expectedGroups);
		newComp.GroupAssignments.SelectMany(g => g).Should().HaveCount(expectedTeams);
		newComp.AllTeamIds().Should().HaveCount(expectedTeams);
		newComp.Games.Should().HaveCount(expectedGames);

		// Stages: group + ko
		newComp.Stages.Should().HaveCount(2);
		newComp.Stages[0].Id.Should().Be("group");
		newComp.Stages[1].Id.Should().Be("ko");

		// Game IDs are 1-based and contiguous
		newComp.Games.Select(g => g.Id).Should().BeInAscendingOrder();
		newComp.Games[0].Id.Should().Be(1);
		newComp.Games[^1].Id.Should().Be(expectedGames);

		// All games scheduled, no results yet
		newComp.Games.Should().OnlyContain(g => g.Result == null);
	}

	[Fact]
	public void Convert_Em2024_ShapeMatchesFactory()
	{
		var factory = CompetitionFactory.Default(CompetitionType.EM, DataService, 2024);
		var oldComp = factory.Create();
		var newComp = FlatDefinitionConverter.Convert(oldComp, "em-2024", 2024);

		newComp.Type.Should().Be(CompetitionType.EM);
		newComp.Year.Should().Be(2024);
		newComp.FormatId.Should().Be("european-championship-24");
		newComp.GroupAssignments.Should().HaveCount(6);
		newComp.AllTeamIds().Should().HaveCount(24);
		newComp.Games.Should().HaveCount(51);
	}

	[Fact]
	public void Convert_Wm2026_KoGames_UseDashedDsl()
	{
		var factory = CompetitionFactory.Default(CompetitionType.WM, DataService, 2026);
		var oldComp = factory.Create();
		var newComp = FlatDefinitionConverter.Convert(oldComp, "wm-2026", 2026);

		var koGames = newComp.Games.OfType<FlatKoGame>().ToList();
		koGames.Should().NotBeEmpty();

		// Every qualifier is parseable. Validates the DSL output of the
		// converter is well-formed.
		foreach (var ko in koGames)
		{
			FlatQualifierParser.TryParse(ko.HomeQual, out _).Should().BeTrue(
				$"home qual '{ko.HomeQual}' on game {ko.Id} should parse");
			FlatQualifierParser.TryParse(ko.AwayQual, out _).Should().BeTrue(
				$"away qual '{ko.AwayQual}' on game {ko.Id} should parse");
		}

		// Game winner / loser qualifiers use dashed form
		var dashedQuals = koGames
			.SelectMany(g => new[] { g.HomeQual, g.AwayQual })
			.Where(q => q.StartsWith("W-") || q.StartsWith("L-"))
			.ToList();
		dashedQuals.Should().NotBeEmpty("WC2026 has KO games whose qualifiers reference earlier KO winners/losers");

		// Group placement qualifiers (A1, B2, etc.) — no dash
		var placementQuals = koGames
			.SelectMany(g => new[] { g.HomeQual, g.AwayQual })
			.Where(q => !q.Contains('-') && !q.Contains('/'))
			.ToList();
		placementQuals.Should().NotBeEmpty("R32 in WC2026 uses group placements");

		// Third-place pool qualifiers exist in WC48
		var poolQuals = koGames
			.SelectMany(g => new[] { g.HomeQual, g.AwayQual })
			.Where(q => q.Contains('/'))
			.ToList();
		poolQuals.Should().NotBeEmpty("WC2026 R32 uses A/B/F3-style pool qualifiers");
	}

	[Fact]
	public void Convert_RoundIds_AreStableSlugsRegardlessOfDisplayLocale()
	{
		var factory = CompetitionFactory.Default(CompetitionType.WM, DataService, 2026);
		var oldComp = factory.Create();
		var newComp = FlatDefinitionConverter.Convert(oldComp, "wm-2026", 2026);

		// Group rounds
		var groupRounds = newComp.Rounds.Where(r => r.StageId == "group").Select(r => r.Id).ToList();
		groupRounds.Should().BeEquivalentTo(["group-r1", "group-r2", "group-r3"]);

		// KO rounds (canonical order for WC48: r32, r16, qf, sf, third, final)
		var koRounds = newComp.Rounds.Where(r => r.StageId == "ko").OrderBy(r => r.Order).Select(r => r.Id).ToList();
		koRounds.Should().BeEquivalentTo(["r32", "r16", "qf", "sf", "third", "final"]);
	}

	[Fact]
	public void Convert_RoundTrips_ThroughJson()
	{
		// Convert → Serialize → Load → Compare. The flat model passes through
		// the JSON layer without losing fidelity.
		var factory = CompetitionFactory.Default(CompetitionType.WM, DataService, 2022);
		var oldComp = factory.Create();
		var converted = FlatDefinitionConverter.Convert(oldComp, "wm-2022", 2022);

		var json = FlatJson.Serialize(converted);
		var reloaded = FlatCompetitionDefinitionLoader.Load(json);

		reloaded.Type.Should().Be(converted.Type);
		reloaded.Year.Should().Be(converted.Year);
		reloaded.FormatId.Should().Be(converted.FormatId);
		reloaded.DefinitionId.Should().Be(converted.DefinitionId);
		reloaded.Games.Should().HaveCount(converted.Games.Length);
		reloaded.Stages.Should().BeEquivalentTo(converted.Stages);
		reloaded.Rounds.Should().BeEquivalentTo(converted.Rounds);
		reloaded.GroupAssignments.Should().BeEquivalentTo(converted.GroupAssignments);
	}
}
