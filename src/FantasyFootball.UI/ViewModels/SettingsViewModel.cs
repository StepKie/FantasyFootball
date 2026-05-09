namespace FantasyFootball.ViewModels;

public partial class SettingsViewModel : GeneralViewModel
{
	public IList<CultureInfo> SupportedLanguages { get; init; }

	[ObservableProperty]
	public partial CultureInfo SelectedLanguage { get; set; }

	[ObservableProperty]
	public partial double SimulationSpeedMs { get; set; }

	[ObservableProperty]
	public partial bool IsBusyA { get; set; }
	public bool IsBusyB { get; set; }

	readonly ISettingsService _settings;
	readonly IDataService _dataService;

	public SettingsViewModel(ISettingsService settingsService, IDataService dataService)
	{
		_settings = settingsService;
		_dataService = dataService;
		SelectedLanguage = settingsService.LastUsedLanguage;

		SimulationSpeedMs = _settings.SimulationSpeed.TotalMilliseconds;
		SupportedLanguages = [new("en"), new("de"),];
	}

	public string AppVersion => AppInfo.VersionString;

	partial void OnSelectedLanguageChanged(CultureInfo value)
	{
		_settings.AddOrUpdateValue("Language", value.Name);
		// TODO Changed in .NET MAUI 10
		// LocalizationResourceManager.Current.CurrentCulture = value;
		CultureInfo.CurrentUICulture = value;
		CultureInfo.DefaultThreadCurrentCulture = value;
		CultureInfo.DefaultThreadCurrentUICulture = value;
	}

	partial void OnSimulationSpeedMsChanged(double value) => _settings.SimulationSpeed = TimeSpan.FromMilliseconds(value);

	[RelayCommand]
	async Task ResetDatabase()
	{
		// TODO The ActivityIndicator will not show on Android: https://github.com/dotnet/maui/issues/8135
		IsBusy = true;
		await Task.Run(_dataService.Reset).ConfigureAwait(false);
		IsBusy = false;
	}
}
