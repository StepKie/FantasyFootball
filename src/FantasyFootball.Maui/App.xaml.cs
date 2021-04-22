using FantasyFootball.Helpers;
using Xamarin.CommunityToolkit.Helpers;

namespace FantasyFootball;

public partial class App : Application
{
	public App()
	{
		LoadLanguage();
		InitializeComponent();
		Log.Logger = ISettingsService.StandardLoggerConfig.CreateLogger();
		ServiceHelper.GetService<IDataService>()!.Reset();
		MainPage = new AppShell();

		Routing.RegisterRoute(nameof(CompetitionDetailPage), typeof(CompetitionDetailPage));
		Routing.RegisterRoute("EM", typeof(CompetitionsPage));
		Routing.RegisterRoute("WM", typeof(CompetitionsPage));
		Routing.RegisterRoute(nameof(CompetitionStatisticsPage), typeof(CompetitionStatisticsPage));
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
		Routing.RegisterRoute(nameof(TeamsPage), typeof(TeamsPage));
		Routing.RegisterRoute(nameof(NewCompetitionPage), typeof(NewCompetitionPage));
	}

	static void LoadLanguage()
	{
		LocalizationResourceManager.Current.PropertyChanged += (sender, e) => Res.Culture = LocalizationResourceManager.Current.CurrentCulture;
		LocalizationResourceManager.Current.Init(Res.ResourceManager);
		LocalizationResourceManager.Current.CurrentCulture = CultureInfo.GetCultureInfo(Preferences.Get("Language", "de"));
	}

	protected override void OnStart()
	{
	}

	protected override void OnSleep()
	{
	}

	protected override void OnResume()
	{
	}
}
