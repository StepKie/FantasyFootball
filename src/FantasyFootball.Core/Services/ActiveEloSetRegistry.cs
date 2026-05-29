namespace FantasyFootball.Services;

/// <summary>
/// <see cref="ITeamRegistry"/> backed by the currently-active EloSet's snapshot —
/// both <c>AllTeamIds</c> and <c>EloOf</c> read from <see cref="IActiveEloSet.Current"/>.
/// Sourcing both members from the same snapshot keeps draws era-consistent: a
/// random-lineup draw can only pick teams the active snapshot covers, so the
/// subsequent <c>EloOf</c> never crashes on a missing team.
/// Contract: <see cref="JsonDataService.Initialize"/> must run before any access —
/// both members throw if <c>Current</c> is null.
/// </summary>
public sealed class ActiveEloSetRegistry : ITeamRegistry
{
	readonly IActiveEloSet _active;

	public ActiveEloSetRegistry(IActiveEloSet active)
	{
		_active = active;
	}

	public IReadOnlyList<string> AllTeamIds => RequireCurrent().Snapshot.Keys.ToList();

	public int EloOf(string teamId)
	{
		var current = RequireCurrent();
		if (!current.Snapshot.TryGetValue(teamId, out var elo))
		{
			throw new KeyNotFoundException($"Team '{teamId}' not in active EloSet '{current.Name}' ({current.Snapshot.Count} entries).");
		}
		return elo;
	}

	EloSet RequireCurrent() => _active.Current ?? throw new InvalidOperationException("No active EloSet.");
}
