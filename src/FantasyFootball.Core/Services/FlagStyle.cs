namespace FantasyFootball.Services;

/// <summary>
/// Persisted visual style for flag rendering, picked on Settings.
/// </summary>
public enum FlagStyle
{
	/// <summary>Rectangular flag, no clip. The default before PR 2 of the polish work.</summary>
	Square,

	/// <summary>Circular clip via clip-path + object-fit cover, with a subtle inset border.</summary>
	Round,

	/// <summary>Circular clip plus an Elo-tiered border (gold ≥1700, silver ≥1500, bronze ≥1300, none below).</summary>
	RoundTier,
}
