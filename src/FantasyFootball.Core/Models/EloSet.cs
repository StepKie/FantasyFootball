namespace FantasyFootball.Models;

/// <summary>
/// Named snapshot of national-team Elo ratings at a point in time.
/// Key is the FIFA 3-letter country code (matches <see cref="Team.ShortName"/>).
/// The simulator's <see cref="ITeamRegistry"/> reads through whichever EloSet
/// is currently active (UI selection) or passed explicitly to a single sim
/// (a HistoricalSpec uses the year-matched snapshot regardless of UI state).
/// </summary>
public class EloSet : NamedUniqueId
{
	public DateOnly Date { get; set; }

	public Dictionary<string, int> Snapshot { get; set; } = [];
}
