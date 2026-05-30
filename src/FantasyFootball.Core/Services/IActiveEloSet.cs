namespace FantasyFootball.Services;

/// <summary>
/// Holds the currently-selected <see cref="EloSet"/> for the UI and the
/// default simulator path. Switched by the Teams page picker (manual)
/// or by the data-service bootstrap (which sets it to the seeded
/// "Current" snapshot on first load).
///
/// HistoricalSpec sims do NOT mutate this — they pass an EloSet directly
/// into the score model via <see cref="ActiveEloSetRegistry"/>'s sister
/// class for sim-only overrides.
/// </summary>
public interface IActiveEloSet
{
	EloSet? Current { get; }

	void SetCurrent(EloSet eloSet);
}
