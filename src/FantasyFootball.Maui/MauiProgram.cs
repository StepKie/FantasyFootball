namespace FantasyFootball;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts => fonts
				.AddFont("OpenSans-Regular.ttf", "OpenSansRegular")
				.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold")
				.AddFont("MaterialIconsOutlined-Regular.otf", "MaterialIcons"));

		builder.Services
			.AddSingleton<IConnectivity>(Connectivity.Current)
			.AddSingleton<ISettingsService, SettingsService>()
			.AddSingleton<IDataService, CsvDataService>()
			.AddSingleton<IRepository>(new Repository(inMemory: false))
			.AddLogging(lb => lb.AddSerilog());

		// Register pages and viewmodels

		builder.Services
			.AddSingleton<CompetitionsPage>()
			.AddSingleton<CompetitionsViewModel>()

			.AddSingleton<CompetitionSetupPage>()
			.AddSingleton<CompetitionSetupViewModel>()

			.AddSingleton<CompetitionStatisticsPage>()
			.AddSingleton<StandingsPage>()
			.AddSingleton<StandingsViewModel>()

			.AddSingleton<SettingsPage>()
			.AddSingleton<SettingsViewModel>()

			.AddSingleton<TeamsPage>()
			.AddSingleton<TeamsViewModel>()

			.AddTransient<CompetitionDetailViewModel>()

			.AddSingleton<GamesPage>()
			.AddSingleton<GamesViewModel>();

		return builder.Build();
	}
}
