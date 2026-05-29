namespace FantasyFootball;

public static class TeamExtensions
{
	/// <summary>
	/// 1-based world rank by Elo descending, resolved through the active EloSet.
	/// <c>OrderByDescending</c> is stable, so ties resolve in roster input order — callers
	/// passing the same roster agree on rank. Returns 0 if not found.
	/// </summary>
	public static int RankByElo(this IEnumerable<Team> roster, IActiveEloSet activeEloSet, int teamId) =>
		roster.OrderByDescending(t => activeEloSet.EloOf(t))
			.Select((t, i) => (t.Id, Rank: i + 1))
			.FirstOrDefault(x => x.Id == teamId)
			.Rank;
}
