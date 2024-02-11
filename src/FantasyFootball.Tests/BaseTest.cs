using System.Globalization;
using FantasyFootball.Repositories;
using FantasyFootball.Services;

namespace FantasyFootball.Tests;

public class BaseTest
{
	protected ITestOutputHelper Output { get; }

	public IRepository Repo { get; }
	public IDataService DataService { get; }
	public BaseTest(ITestOutputHelper output, LogEventLevel level = LogEventLevel.Debug)
	{
		Output = output;
		// Connect global logger
		Log.Logger = ISettingsService.StandardLoggerConfig.WriteTo.TestOutput(Output, level).CreateLogger();
		Repo = new Repository(inMemory: true);
		DataService = new CsvDataService(Repo, new CultureInfo("de"));
	}

	/// <summary> For tests, create a default competition using the Factory class </summary>
	public Competition InitCompetition(CompetitionType type, int year)
	{
		var factory = CompetitionFactory.Default(type, DataService, year);
		var competition = factory.Create();
		Repo.Save(competition);

		var competitions = Repo.GetAll<Competition>();
		Assert.Single(competitions);
		return competitions.First();
	}
}
