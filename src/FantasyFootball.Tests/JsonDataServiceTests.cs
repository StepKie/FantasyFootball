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

		// Bundesliga seed has 18 clubs; if any are added (Champions League, etc.) this should still be ≥ 18.
		clubs.Should().HaveCountGreaterThanOrEqualTo(18, "the Bundesliga seed alone has 18 clubs");
		clubs.Select(c => c.ShortName).Should().Contain(["FCB", "BVB", "RBL", "B04"]);

		var bayern = clubs.Single(c => c.ShortName == "FCB");
		bayern.Country.Code3.Should().Be("GER");
		bayern.IsNationalTeam.Should().BeFalse();
	}
}
