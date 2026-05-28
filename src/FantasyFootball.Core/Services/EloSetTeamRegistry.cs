namespace FantasyFootball.Services;

/// <summary>
/// <see cref="ITeamRegistry"/> backed by a fixed <see cref="EloSet"/> snapshot.
/// Used for per-sim historical overrides — a HistoricalSpec sim wants its
/// year-matched snapshot, not whichever set the user has active in the UI.
/// </summary>
public sealed class EloSetTeamRegistry : ITeamRegistry
{
	readonly EloSet _set;

	public EloSetTeamRegistry(EloSet set) { _set = set; }

	public IReadOnlyList<string> AllTeamIds => _set.Snapshot.Keys.ToList();

	public int EloOf(string teamId)
	{
		if (!_set.Snapshot.TryGetValue(teamId, out var elo))
		{
			throw new KeyNotFoundException($"Team '{teamId}' not in EloSet '{_set.Name}' ({_set.Snapshot.Count} entries).");
		}
		return elo;
	}
}
