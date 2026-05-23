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

		// Competition pages removed; Teams + Settings still registered below.
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
