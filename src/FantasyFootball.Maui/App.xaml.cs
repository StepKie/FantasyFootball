using Xamarin.CommunityToolkit.Helpers;

namespace FantasyFootball;

public partial class App : Application
{
	public App()
	{
		LoadLanguage();
		InitializeComponent();
		Log.Logger = ISettingsService.StandardLoggerConfig.CreateLogger();

		// TODO: Do we need explicit initialization of DataService on app start?
		MainPage = new AppShell();

		Routing.RegisterRoute(nameof(CompetitionsPage), typeof(CompetitionsPage));
		Routing.RegisterRoute(nameof(CompetitionSetupPage), typeof(CompetitionSetupPage));
		Routing.RegisterRoute(nameof(GamesPage), typeof(GamesPage));
		Routing.RegisterRoute(nameof(StandingsPage), typeof(StandingsPage));
		Routing.RegisterRoute(nameof(CompetitionStatisticsPage), typeof(CompetitionStatisticsPage));
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
		Routing.RegisterRoute(nameof(TeamsPage), typeof(TeamsPage));
		Routing.RegisterRoute(nameof(TeamDetailPage), typeof(TeamDetailPage));
	}

	static void LoadLanguage()
	{
		LocalizationResourceManager.Current.PropertyChanged += (sender, e) => Res.Culture = LocalizationResourceManager.Current.CurrentCulture;
		LocalizationResourceManager.Current.Init(Res.ResourceManager);
		LocalizationResourceManager.Current.CurrentCulture = CultureInfo.GetCultureInfo(Preferences.Get("Language", "de"));
	}
}
