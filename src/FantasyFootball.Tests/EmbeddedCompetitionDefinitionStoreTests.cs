using System.IO;

namespace FantasyFootball.Tests;

/// <summary>
/// Tests for the embedded-resource definition store: confirms the
/// committed JSON files land at the right manifest names, load cleanly
/// via the JSON loader, and produce Competitions with the expected
/// shape (group / team / game counts) for each historical format.
/// </summary>
public class EmbeddedCompetitionDefinitionStoreTests
{
	readonly EmbeddedCompetitionDefinitionStore _store = new();

	[Fact]
	public void AvailableIds_ContainsAllHistoricalCompetitions()
	{
		_store.AvailableIds.Should().Contain(["wm-2022", "wm-2026", "em-2024"]);
	}

	[Theory]
	[InlineData("wm-2022", CompetitionType.WM, 2022, 8, 32, 64, "world-cup-32")]
	[InlineData("wm-2026", CompetitionType.WM, 2026, 12, 48, 104, "world-cup-48")]
	[InlineData("em-2024", CompetitionType.EM, 2024, 6, 24, 51, "european-championship-24")]
	// League formats have 0 groups and store participants in Competition.Teams instead.
	[InlineData("bundesliga-2025-2026", CompetitionType.DOMESTIC_LEAGUE, 2026, 0, 18, 306, "bundesliga-18")]
	public void Load_HistoricalCompetition_HasExpectedShape(
		string id, CompetitionType type, int year, int groups, int teams, int games, string formatId)
	{
		var c = _store.Load(id);

		c.DefinitionId.Should().Be(id);
		c.Type.Should().Be(type);
		c.Year.Should().Be(year);
		c.FormatId.Should().Be(formatId);
		c.GroupAssignments.Should().HaveCount(groups);
		c.AllTeamIds().Should().HaveCount(teams);
		c.Games.Should().HaveCount(games);
	}

	[Fact]
	public void Load_UnknownDefinition_ThrowsFileNotFound()
	{
		Action act = () => _store.Load("does-not-exist");
		act.Should().Throw<FileNotFoundException>()
			.WithMessage("*does-not-exist*");
	}

	[Fact]
	public void Load_KoQualifiers_AllParseable()
	{
		// Spot-check: every KO qualifier on every loaded competition
		// parses via QualifierParser. This catches DSL drift between
		// the generator and the parser if either side changes.
		foreach (var id in _store.AvailableIds)
		{
			var c = _store.Load(id);
			var koGames = c.Games.OfType<KoGame>().ToList();
			foreach (var ko in koGames)
			{
				QualifierParser.TryParse(ko.HomeQual, out _).Should().BeTrue(
					$"{id} game {ko.Id} home qual '{ko.HomeQual}' should parse");
				QualifierParser.TryParse(ko.AwayQual, out _).Should().BeTrue(
					$"{id} game {ko.Id} away qual '{ko.AwayQual}' should parse");
			}
		}
	}
}
