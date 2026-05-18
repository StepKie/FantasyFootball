using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FantasyFootball.Services;

namespace FantasyFootball.UI.ViewModels;

/// <summary>
/// Settings page view-model. Lives in the shared UI library so the same instance
/// works in both Blazor WASM and the future MAUI BlazorWebView host.
///
/// Uses constructor DI (no service-locator). The MAUI XAML version of this VM
/// still exists in the FantasyFootball.Maui project and continues to back the
/// XAML SettingsPage until the BlazorWebView host (Phase 5) takes over.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
	readonly ISettingsService _settings;
	readonly IDataService _dataService;

	public SettingsViewModel(ISettingsService settings, IDataService dataService)
	{
		_settings = settings;
		_dataService = dataService;

		SelectedLanguage = settings.LastUsedLanguage;
		SimulationSpeedMs = settings.SimulationSpeed.TotalMilliseconds;
		SupportedLanguages = [new("en"), new("de")];
	}

	public IList<CultureInfo> SupportedLanguages { get; }

	[ObservableProperty]
	public partial CultureInfo SelectedLanguage { get; set; }

	[ObservableProperty]
	public partial double SimulationSpeedMs { get; set; }

	[ObservableProperty]
	public partial bool IsBusy { get; set; }

	// GetEntryAssembly returns the host (FantasyFootball.Web or FantasyFootball.Maui),
	// not this Razor library — so the displayed version reflects the running app.
	public string AppVersion => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "dev";

	partial void OnSelectedLanguageChanged(CultureInfo value)
	{
		_settings.LastUsedLanguage = value;
		// TODO: applying the culture to running localized strings in Blazor WASM requires
		// a separate i18n strategy (e.g., IStringLocalizer + StateHasChanged broadcasting).
		// Tracked as a follow-up; for now the choice persists but only takes effect on reload.
	}

	partial void OnSimulationSpeedMsChanged(double value)
		=> _settings.SimulationSpeed = TimeSpan.FromMilliseconds(value);

	[RelayCommand]
	async Task ResetDatabase()
	{
		IsBusy = true;
		try
		{
			await Task.Run(_dataService.Reset).ConfigureAwait(false);
		}
		finally
		{
			IsBusy = false;
		}
	}
}
