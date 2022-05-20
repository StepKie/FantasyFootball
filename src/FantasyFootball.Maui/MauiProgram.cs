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
			.AddLogging(lb => lb.AddSerilog(dispose: true));

		// Register pages and viewmodels

		builder.Services
			.AddSingleton<CompetitionsPage>()
			.AddSingleton<CompetitionsViewModel>()

			.AddSingleton<CompetitionStatisticsPage>()
			.AddSingleton<StandingsViewModel>()

			.AddSingleton<SettingsPage>()
			.AddSingleton<SettingsViewModel>()

			.AddSingleton<TeamsPage>()
			.AddSingleton<TeamsViewModel>()

			.AddTransient<CompetitionDetailPage>()
			.AddTransient<CompetitionDetailViewModel>()

			.AddTransient<NewCompetitionPage>()
			.AddTransient<NewCompetitionViewModel>()

			.AddSingleton<GamesPage>()
			.AddSingleton<GamesViewModel>()

			.AddSingleton<StandingsPage>();

		return builder.Build();
	}
}
