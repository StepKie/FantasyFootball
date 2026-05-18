using FantasyFootball.Models;

namespace FantasyFootball.UI.Helpers;

public static class RankingExtensions
{
  /// <summary>
  /// Returns the 1-based world rank of <paramref name="teamId"/> within
  /// <paramref name="roster"/>, ordered by Elo descending. LINQ
  /// <c>OrderByDescending</c> is stable, so tied Elos resolve in the roster's
  /// input order — callers passing the same roster agree on rank. The Teams
  /// list builds ranks via the equivalent <c>OrderByDescending().Select((t, i) => i + 1)</c>
  /// projection, so list and detail pages stay in sync. Returns 0 if the team
  /// isn't in the roster.
  /// </summary>
  public static int RankByElo(this IEnumerable<Team> roster, int teamId) =>
    roster.OrderByDescending(t => t.Elo)
      .Select((t, i) => (t.Id, Rank: i + 1))
      .FirstOrDefault(x => x.Id == teamId)
      .Rank;
}
