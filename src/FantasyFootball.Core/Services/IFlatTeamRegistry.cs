namespace FantasyFootball.Services;

/// <summary>
/// The pool of teams a random-lineup draw can pull from, plus the
/// per-team strength data the score model consumes. Starts as the
/// global FIFA-ranked roster; later may be narrowed (by region,
/// confederation, qualifying-status filters).
///
/// Decoupled from the old <c>DataService</c> so the new bulk-sim path
/// doesn't drag along graph-model concerns; a future <c>flat</c>
/// version of the team registry can implement this directly.
/// </summary>
public interface IFlatTeamRegistry
{
	/// <summary>All team IDs currently in the pool.</summary>
	IReadOnlyList<string> AllTeamIds { get; }

	/// <summary>
	/// FIFA-style ELO rating for the given team ID. Throws if the team
	/// isn't in the pool — callers should hand IDs from this registry
	/// directly, not external sources.
	/// </summary>
	int EloOf(string teamId);
}
