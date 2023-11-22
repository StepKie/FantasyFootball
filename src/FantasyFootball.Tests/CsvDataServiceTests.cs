using FantasyFootball.Services;

namespace FantasyFootball.Tests;

public class CsvDataServiceTests(ITestOutputHelper output) : BaseTest(output, level: LogEventLevel.Debug)
{
	[Fact]
	public void TestCsvImport()
	{
		Assert.True(DataService is CsvDataService);
		var teams = DataService.CreateTeams();
		teams.Should().NotBeNullOrEmpty();
	}
}
