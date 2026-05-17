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
    get => CultureInfo.GetCultureInfo(GetValueOrDefault(LanguageKey, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));
    set => AddOrUpdateValue(LanguageKey, value?.TwoLetterISOLanguageName ?? "en");
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
