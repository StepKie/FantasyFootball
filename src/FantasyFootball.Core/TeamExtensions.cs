namespace FantasyFootball;

public static class TeamExtensions
{
	/// <summary>
	/// 1-based rank of <paramref name="teamId"/> in the roster ordered by elo descending under
	/// <paramref name="set"/>. Missing teams get elo 0. <c>OrderByDescending</c> is stable, so
	/// ties resolve in roster input order. Returns 0 when the team isn't in the roster.
	/// </summary>
	public static int RankByElo(this IEnumerable<Team> roster, EloSet set, int teamId) =>
		roster.OrderByDescending(t => set.Snapshot.GetValueOrDefault(t.ShortName))
			.Select((t, i) => (t.Id, Rank: i + 1))
			.FirstOrDefault(x => x.Id == teamId)
			.Rank;
}
