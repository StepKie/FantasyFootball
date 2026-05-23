namespace FantasyFootball;

public partial class App : Application
{
	public App()
	{
		LoadLanguage();
		InitializeComponent();

		// TODO Force Light AppTheme until there is time to fine-tune the AppThemeBindings
		Current!.UserAppTheme = AppTheme.Light;
		Log.Logger = ISettingsService.StandardLoggerConfig.CreateLogger();

		ServiceHelper.GetService<IDataService>()!.Initialize();

		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
		Routing.RegisterRoute(nameof(TeamsPage), typeof(TeamsPage));
		Routing.RegisterRoute(nameof(TeamDetailPage), typeof(TeamDetailPage));
	}

	protected override Window CreateWindow(IActivationState? activationState) => new(new AppShell());

	static void LoadLanguage()
	{
		// TODO FIXME API gone with .NET MAUI 10
		//LocalizationResourceManager.Current.PropertyChanged += (_, _) => Res.Culture = LocalizationResourceManager.Current.CurrentCulture;
		//LocalizationResourceManager.Current.Init(Res.ResourceManager);
		//LocalizationResourceManager.Current.CurrentCulture = CultureInfo.GetCultureInfo(Preferences.Get("Language", "de"));
	}
}
