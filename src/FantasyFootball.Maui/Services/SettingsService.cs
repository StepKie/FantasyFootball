namespace FantasyFootball.Services;

public class SettingsService : ISettingsService
{
	public const string idConfigKey = "id_token";
	public const string idSimSpeedMs = "id_simspeed";
	public const string idLanguage = "id_language";

	static readonly string _codeDefaultLanguage = (Resources.AppResources.Culture ?? Thread.CurrentThread.CurrentUICulture).TwoLetterISOLanguageName;

	#region Settings Properties

	public string LastUsedCompetition
	{
		get => GetValueOrDefault(idConfigKey, "");
		set => AddOrUpdateValue(idConfigKey, value);
	}
	public TimeSpan SimulationSpeed
	{
		get => TimeSpan.FromMilliseconds(GetValueOrDefault(idSimSpeedMs, 100));
		set => AddOrUpdateValue(idSimSpeedMs, value.TotalMilliseconds);
	}

	public CultureInfo LastUsedLanguage
	{
		get => CultureInfo.GetCultureInfo(GetValueOrDefault(idLanguage, _codeDefaultLanguage));
		set => AddOrUpdateValue(idLanguage, value?.TwoLetterISOLanguageName ?? idLanguage);
	}

	#endregion

	#region Public Methods

	public void AddOrUpdateValue(string key, bool value) => Preferences.Set(key, value);
	public void AddOrUpdateValue(string key, string value) => Preferences.Set(key, value);
	public void AddOrUpdateValue(string key, double value) => Preferences.Set(key, value);
	public bool GetValueOrDefault(string key, bool defaultValue) => Preferences.Get(key, defaultValue);
	public string GetValueOrDefault(string key, string defaultValue) => Preferences.Get(key, defaultValue);
	public double GetValueOrDefault(string key, double defaultValue) => Preferences.Get(key, defaultValue);

	#endregion
}
