namespace FantasyFootball.Services;

/// <summary>
/// Produces a group-stage lineup for a <see cref="Models.RandomLineupSpec"/>.
/// Stateless contract — same inputs may yield different outputs across
/// calls; the runner reseeds per run.
///
/// Implementations:
/// <list type="bullet">
///   <item><see cref="UniformDrawFromRegistry"/> — pulls uniformly at
///     random from the global team registry, no FIFA-pot constraints.</item>
///   <item>Future: <c>FifaPotsDraw</c> mirroring real-world drawing rules
///     (pot 1 = top-seeded, no two teams from same confederation per
///     group, etc.).</item>
/// </list>
/// </summary>
public interface IDrawAlgorithm
{
	/// <summary>
	/// Draws a lineup. Returns a jagged array indexed by group letter:
	/// <c>result[i]</c> holds <paramref name="teamsPerGroup"/> distinct
	/// team IDs for group <c>(char)('A' + i)</c>.
	/// </summary>
	string[][] Draw(int groupCount, int teamsPerGroup);
}
