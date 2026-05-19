using Blazored.LocalStorage;
using FantasyFootball.Services;

namespace FantasyFootball.Web.Services;

/// <summary>
/// Browser LocalStorage-backed ISettingsService. Mirrors the key names used by
/// the MAUI <see cref="SettingsService"/> so a user migrating between hosts (or
/// the future MAUI BlazorWebView host sharing the same component tree) sees the
/// same preferences.
/// </summary>
public sealed class LocalStorageSettingsService : ISettingsService
{
  const string LanguageKey = "id_language";
  const string SimSpeedKey = "id_simspeed";
  const string LastCompetitionKey = "id_token";
  const string FlagStyleKey = "id_flagstyle";
  const string UseOfficialLogosKey = "id_useofficiallogos";

  readonly ISyncLocalStorageService _localStorage;

  public LocalStorageSettingsService(ISyncLocalStorageService localStorage)
  {
    _localStorage = localStorage;
  }

  public string LastUsedCompetition
  {
    get => GetValueOrDefault(LastCompetitionKey, "");
    set => AddOrUpdateValue(LastCompetitionKey, value);
  }

  public TimeSpan SimulationSpeed
  {
    get => TimeSpan.FromMilliseconds(GetValueOrDefault(SimSpeedKey, 100d));
    set => AddOrUpdateValue(SimSpeedKey, value.TotalMilliseconds);
  }

  public CultureInfo LastUsedLanguage
  {
    get => CultureInfo.GetCultureInfo(GetValueOrDefault(LanguageKey, "en"));
    set => AddOrUpdateValue(LanguageKey, value?.TwoLetterISOLanguageName ?? "en");
  }

  // New-user default is Round — the visible polish improvement over Square.
  public FlagStyle FlagStyle
  {
    get => Enum.TryParse<FlagStyle>(GetValueOrDefault(FlagStyleKey, ""), out var s) ? s : FlagStyle.Round;
    set => AddOrUpdateValue(FlagStyleKey, value.ToString());
  }

  // Off by default — generic icons ship with the repo; user drops official assets in wwwroot/competition-icons/official/ locally and flips this.
  public bool UseOfficialCompetitionLogos
  {
    get => GetValueOrDefault(UseOfficialLogosKey, false);
    set => AddOrUpdateValue(UseOfficialLogosKey, value);
  }

  public bool GetValueOrDefault(string key, bool defaultValue)
    => _localStorage.ContainKey(key) ? _localStorage.GetItem<bool>(key) : defaultValue;

  public string GetValueOrDefault(string key, string defaultValue)
    => _localStorage.ContainKey(key) ? _localStorage.GetItem<string>(key) ?? defaultValue : defaultValue;

  public double GetValueOrDefault(string key, double defaultValue)
    => _localStorage.ContainKey(key) ? _localStorage.GetItem<double>(key) : defaultValue;

  public void AddOrUpdateValue(string key, bool value) => _localStorage.SetItem(key, value);
  public void AddOrUpdateValue(string key, string value) => _localStorage.SetItem(key, value);
  public void AddOrUpdateValue(string key, double value) => _localStorage.SetItem(key, value);
}
