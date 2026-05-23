namespace FantasyFootball.Services;

/// <summary>
/// Picks teams uniformly at random from an <see cref="ITeamRegistry"/>
/// without replacement. No seeding pots, no confederation constraints —
/// the simplest possible draw. Real-world FIFA rules (pots,
/// confederation exclusions) belong in a separate implementation.
/// </summary>
public sealed class UniformDrawFromRegistry : IDrawAlgorithm
{
	readonly ITeamRegistry _registry;
	readonly Random _rng;

	public UniformDrawFromRegistry(ITeamRegistry registry, Random? rng = null)
	{
		_registry = registry;
		_rng = rng ?? Random.Shared;
	}

	public string[][] Draw(int groupCount, int teamsPerGroup)
	{
		var total = groupCount * teamsPerGroup;
		var pool = _registry.AllTeamIds;
		if (pool.Count < total)
		{
			throw new InvalidOperationException(
				$"Team registry has only {pool.Count} teams; draw needs {total} ({groupCount} groups × {teamsPerGroup}).");
		}

		// Fisher-Yates partial shuffle: pick `total` distinct teams from
		// the pool without materializing an entire shuffled copy.
		var picked = pool.ToArray();
		for (int i = 0; i < total; i++)
		{
			var j = _rng.Next(i, picked.Length);
			(picked[i], picked[j]) = (picked[j], picked[i]);
		}

		var groups = new string[groupCount][];
		for (int g = 0; g < groupCount; g++)
		{
			var members = new string[teamsPerGroup];
			Array.Copy(picked, g * teamsPerGroup, members, 0, teamsPerGroup);
			groups[g] = members;
		}
		return groups;
	}
}
