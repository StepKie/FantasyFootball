using FantasyFootball.Services;

namespace FantasyFootball.Tests;

public class JsonDataServiceTests(ITestOutputHelper output) : BaseTest(output, level: LogEventLevel.Debug)
{
	[Fact]
	public void TestJsonImport()
	{
		Assert.True(DataService is JsonDataService);
		var teams = DataService.CreateTeams();
		teams.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void ClubTeams_LoadedFromClubsJson()
	{
		var clubs = DataService.AllTeams.Where(t => t.Type == TeamType.CLUB_MEN).ToList();

		// All five leagues seed their clubs: 18 + 20 + 20 + 20 + 18 = 96.
		clubs.Should().HaveCountGreaterThanOrEqualTo(96, "all five domestic leagues seed their clubs");
		clubs.Select(c => c.ShortName).Should().Contain(["FCB", "ARS", "RMA", "JUV", "PSG"]);

		var bayern = clubs.Single(c => c.ShortName == "FCB");
		bayern.Country.Code3.Should().Be("GER");
		bayern.IsNationalTeam.Should().BeFalse();
	}

	[Fact]
	public void BundledEloSetNames_UsesActualNames_NoNulls()
	{
		JsonDataService.BundledEloSetNames.Should().NotContainNulls();
		JsonDataService.BundledEloSetNames.Should().Contain([JsonDataService.CurrentEloSetName, "Clubs 2025-2026"]);
	}
}
