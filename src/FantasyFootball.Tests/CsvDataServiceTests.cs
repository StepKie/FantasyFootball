using FantasyFootball.Services;

namespace FantasyFootball.Tests;

public class CsvDataServiceTests : BaseTest
{
	public CsvDataServiceTests(ITestOutputHelper output) : base(output, level: LogEventLevel.Debug) { }

	[Fact]
	public void TestCsvImport()
	{
		Assert.True(DataService is CsvDataService);
		var teams = DataService.CreateTeams();
		Assert.NotEmpty(teams);
	}
}
