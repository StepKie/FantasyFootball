using System.Globalization;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using Serilog.Sinks.XUnit3;

namespace FantasyFootball.Tests;

public class BaseTest
{
	protected ITestOutputHelper Output { get; }

	public IRepository Repo { get; }
	public IDataService DataService { get; }

	public BaseTest(ITestOutputHelper output, LogEventLevel level = LogEventLevel.Debug)
	{
		Output = output;
		Log.Logger = ISettingsService.StandardLoggerConfig.MinimumLevel.Is(level).WriteTo.XUnit3TestOutput().CreateLogger();
		Repo = new Repository(inMemory: true);
		DataService = new JsonDataService(Repo, new CultureInfo("de"));
	}
}
