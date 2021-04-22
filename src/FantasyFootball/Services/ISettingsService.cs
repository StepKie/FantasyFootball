namespace FantasyFootball.Services;

/// <summary>
/// Service responsible for persisting user settings between app starts
/// Possible use cases are last page visited, last config used, form fields to be hidden, selected app theme (dark/light) etc.
/// </summary>
public interface ISettingsService
{
	/// <summary> Provides a standard LoggerConfiguration that can be enriched with additional sinks before calling CreateLogger() </summary>
	public static LoggerConfiguration StandardLoggerConfig => new LoggerConfiguration().MinimumLevel.Debug().Enrich.FromLogContext().WriteTo.Debug();

	string LastUsedCompetition { get; set; }
	TimeSpan SimulationSpeed { get; set; }
	public CultureInfo LastUsedLanguage { get; set; }

	bool GetValueOrDefault(string key, bool defaultValue);
	string GetValueOrDefault(string key, string defaultValue);
	double GetValueOrDefault(string key, double defaultValue);
	void AddOrUpdateValue(string key, bool value);
	void AddOrUpdateValue(string key, string value);
	void AddOrUpdateValue(string key, double value);
}
