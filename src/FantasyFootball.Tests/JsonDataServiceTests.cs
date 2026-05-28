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
}
