using System.IO;
using FantasyFootball.Data.CompetitionFactories;
using FantasyFootball.Services;

namespace FantasyFootball.Tests;

/// <summary>
/// One-off generator: runs the old factory → flat converter → JSON serializer
/// pipeline for each historical competition and writes the JSON definition
/// file to <c>src/FantasyFootball.Core/Definitions/</c>, where it gets
/// committed as an embedded resource.
///
/// Excluded from the normal test suite via the
/// <c>GenerateDefinitions</c> trait. To regenerate:
/// <code>dotnet test --filter "Category=GenerateDefinitions"</code>
///
/// This whole class disappears in the cleanup PR alongside the old
/// factories — at that point the committed JSON files are the source
/// of truth and there's nothing to regenerate from.
/// </summary>
public class GenerateDefinitionsTest : BaseTest
{
	public GenerateDefinitionsTest(ITestOutputHelper output) : base(output) { }

	static readonly string OutputDir = Path.GetFullPath(Path.Combine(
		Path.GetDirectoryName(typeof(GenerateDefinitionsTest).Assembly.Location)!,
		"..", "..", "..", "..", "FantasyFootball.Core", "Definitions"));

	[Fact]
	[Trait("Category", "GenerateDefinitions")]
	public void Generate_AllHistoricalCompetitions()
	{
		Directory.CreateDirectory(OutputDir);

		WriteDefinition(CompetitionType.WM, 2022, "wm-2022");
		WriteDefinition(CompetitionType.WM, 2026, "wm-2026");
		WriteDefinition(CompetitionType.EM, 2024, "em-2024");

		Output.WriteLine($"Wrote definitions to: {OutputDir}");
	}

	void WriteDefinition(CompetitionType type, int year, string definitionId)
	{
		var factory = CompetitionFactory.Default(type, DataService, year);
		var oldComp = factory.Create();
		var flat = FlatDefinitionConverter.Convert(oldComp, definitionId, year);
		var json = FlatJson.Serialize(flat);
		var path = Path.Combine(OutputDir, $"{definitionId}.json");
		File.WriteAllText(path, json);
		Output.WriteLine($"  - {definitionId}.json: {flat.Games.Length} games, {flat.AllTeamIds().Count()} teams");
	}
}
