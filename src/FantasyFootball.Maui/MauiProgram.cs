using CommunityToolkit.Maui;

namespace FantasyFootball;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
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

		// Register pages and viewmodels. Competition-related pages were removed during
		// the graph-model → flat-model cleanup; rebuilding them against the flat model
		// is a separate follow-up. Teams + Settings still work.

		builder.Services
			.AddSingleton<SettingsPage>()
			.AddSingleton<SettingsViewModel>()

			.AddSingleton<TeamsPage>()
			.AddSingleton<TeamsViewModel>()

			.AddTransient<TeamDetailPage>()
			.AddTransient<TeamViewModel>();

		return builder.Build();
	}
}
