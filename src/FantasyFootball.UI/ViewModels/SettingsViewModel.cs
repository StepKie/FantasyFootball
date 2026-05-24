using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FantasyFootball.Repositories;
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
	readonly ICompetitionRepository _repo;

	public SettingsViewModel(
		ISettingsService settings,
		IDataService dataService,
		ICompetitionRepository repo)
	{
		_settings = settings;
		_dataService = dataService;
		_repo = repo;

		SelectedLanguage = settings.LastUsedLanguage;
		SelectedSimulationSpeed = SimulationSpeedExtensions.FromTimeSpan(settings.SimulationSpeed);
		SelectedFlagStyle = settings.FlagStyle;
		UseOfficialCompetitionLogos = settings.UseOfficialCompetitionLogos;
		ShowFullStandings = settings.ShowFullStandings;
		SupportedLanguages = [new("en"), new("de")];
	}

	public IList<CultureInfo> SupportedLanguages { get; }

	[ObservableProperty]
	public partial CultureInfo SelectedLanguage { get; set; }

	[ObservableProperty]
	public partial SimulationSpeed SelectedSimulationSpeed { get; set; }

	[ObservableProperty]
	public partial FlagStyle SelectedFlagStyle { get; set; }

	[ObservableProperty]
	public partial bool UseOfficialCompetitionLogos { get; set; }

	[ObservableProperty]
	public partial bool ShowFullStandings { get; set; }

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

	partial void OnSelectedSimulationSpeedChanged(SimulationSpeed value)
		=> _settings.SimulationSpeed = value.ToDelay();

	partial void OnSelectedFlagStyleChanged(FlagStyle value)
		=> _settings.FlagStyle = value;

	partial void OnUseOfficialCompetitionLogosChanged(bool value)
		=> _settings.UseOfficialCompetitionLogos = value;

	partial void OnShowFullStandingsChanged(bool value)
		=> _settings.ShowFullStandings = value;

	[RelayCommand]
	async Task ResetDatabase()
	{
		IsBusy = true;
		try
		{
			await Task.Run(_dataService.Reset).ConfigureAwait(false);
			await _repo.ResetAsync().ConfigureAwait(false);
		}
		finally
		{
			IsBusy = false;
		}
	}
}
