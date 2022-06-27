using Xamarin.CommunityToolkit.Helpers;

namespace FantasyFootball;

public partial class App : Application
{
	public App()
	{
		LoadLanguage();
		InitializeComponent();
		Log.Logger = ISettingsService.StandardLoggerConfig.CreateLogger();

		var repoService = ServiceHelper.GetService<IRepository>()!;
		if (!repoService.GetAll<Team>().Any())
		{
			ServiceHelper.GetService<IDataService>()!.Reset();
		}

		MainPage = new AppShell();

		Routing.RegisterRoute(nameof(CompetitionsPage), typeof(CompetitionsPage));
		Routing.RegisterRoute(nameof(CompetitionSetupPage), typeof(CompetitionSetupPage));
		Routing.RegisterRoute(nameof(GamesPage), typeof(GamesPage));
		Routing.RegisterRoute(nameof(StandingsPage), typeof(StandingsPage));
		Routing.RegisterRoute(nameof(CompetitionStatisticsPage), typeof(CompetitionStatisticsPage));
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
		Routing.RegisterRoute(nameof(TeamsPage), typeof(TeamsPage));
	}

	static void LoadLanguage()
	{
		LocalizationResourceManager.Current.PropertyChanged += (sender, e) => Res.Culture = LocalizationResourceManager.Current.CurrentCulture;
		LocalizationResourceManager.Current.Init(Res.ResourceManager);
		LocalizationResourceManager.Current.CurrentCulture = CultureInfo.GetCultureInfo(Preferences.Get("Language", "de"));
	}
}
