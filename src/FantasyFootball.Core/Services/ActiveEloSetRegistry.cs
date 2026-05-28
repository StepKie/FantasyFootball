namespace FantasyFootball.Services;

/// <summary>
/// <see cref="ITeamRegistry"/> backed by the currently-active EloSet's
/// snapshot. EloOf reads through <see cref="IActiveEloSet.Current"/>;
/// AllTeamIds still comes from the data service (the pool of teams a
/// random-lineup draw can pull from is independent of which EloSet is
/// active).
/// </summary>
public sealed class ActiveEloSetRegistry : ITeamRegistry
{
	readonly IActiveEloSet _active;
	readonly IDataService _data;
	IReadOnlyList<string>? _teamIds;

	public ActiveEloSetRegistry(IActiveEloSet active, IDataService data)
	{
		_active = active;
		_data = data;
	}

	public IReadOnlyList<string> AllTeamIds => _teamIds ??= _data.AllTeams.Select(t => t.ShortName).ToList();

	public int EloOf(string teamId)
	{
		var current = _active.Current ?? throw new InvalidOperationException(
			"No active EloSet — IDataService.Initialize() must run before any EloOf lookup.");
		if (!current.Snapshot.TryGetValue(teamId, out var elo))
		{
			throw new KeyNotFoundException($"Team '{teamId}' not in active EloSet '{current.Name}' ({current.Snapshot.Count} entries).");
		}
		return elo;
	}
}
