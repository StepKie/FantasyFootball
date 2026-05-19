namespace FantasyFootball.Services;

/// <summary>
/// Coarse-grained speed presets exposed on the competition detail page,
/// distinct from <see cref="ISettingsService.SimulationSpeed"/> which is
/// a free-form TimeSpan global default. The page lets the user pick a
/// preset for the current visit without touching Settings.
/// </summary>
public enum SimulationSpeed
{
	Slow,
	Normal,
	Fast,
	/// <summary>
	/// Skip the inter-game delay entirely and suppress per-game re-render
	/// notifications. The whole batch (Round / Stage / Tournament) renders
	/// once at the end — fast even for the 48-team Expanded World Cup.
	/// </summary>
	Instant,
}

public static class SimulationSpeedExtensions
{
	public static TimeSpan ToDelay(this SimulationSpeed speed) => speed switch
	{
		SimulationSpeed.Slow => TimeSpan.FromMilliseconds(500),
		SimulationSpeed.Normal => TimeSpan.FromMilliseconds(100),
		SimulationSpeed.Fast => TimeSpan.FromMilliseconds(20),
		SimulationSpeed.Instant => TimeSpan.Zero,
		_ => throw new ArgumentOutOfRangeException(nameof(speed), speed, null),
	};

	/// <summary>
	/// Pick the closest preset for a Settings <see cref="TimeSpan"/> so the
	/// page's speed control starts on a sensible bucket.
	/// </summary>
	public static SimulationSpeed FromTimeSpan(TimeSpan span) => span.TotalMilliseconds switch
	{
		<= 5 => SimulationSpeed.Instant,
		< 50 => SimulationSpeed.Fast,
		< 250 => SimulationSpeed.Normal,
		_ => SimulationSpeed.Slow,
	};
}
