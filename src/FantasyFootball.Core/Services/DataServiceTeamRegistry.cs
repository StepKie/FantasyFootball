using FantasyFootball.Data;

namespace FantasyFootball.Services;

/// <summary>
/// Bridges the new flat path to the existing <see cref="IDataService"/>
/// (which owns the CSV-loaded ELO data). Transitional — disappears in
/// the cleanup PR when a native flat teams.json registry replaces it.
///
/// Caches the team lookup table after the first read so repeated
/// <c>EloOf</c> calls during simulation are O(1).
/// </summary>
public sealed class DataServiceTeamRegistry : IFlatTeamRegistry
{
	readonly IDataService _data;
	Dictionary<string, int>? _eloByShortName;
	IReadOnlyList<string>? _teamIds;

	public DataServiceTeamRegistry(IDataService data)
	{
		_data = data;
	}

	public IReadOnlyList<string> AllTeamIds
	{
		get
		{
			EnsureCache();
			return _teamIds!;
		}
	}

	public int EloOf(string teamId)
	{
		EnsureCache();
		if (!_eloByShortName!.TryGetValue(teamId, out var elo))
		{
			throw new KeyNotFoundException($"Team '{teamId}' is not in the registry. Loaded teams: {_eloByShortName.Count}.");
		}
		return elo;
	}

	void EnsureCache()
	{
		if (_eloByShortName is not null) { return; }
		var teams = _data.AllTeams.ToList();
		_eloByShortName = teams.ToDictionary(t => t.ShortName, t => t.Elo);
		_teamIds = teams.Select(t => t.ShortName).ToList();
	}
}
