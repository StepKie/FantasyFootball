namespace FantasyFootball.Data;

/// <summary> Evolved from a full-class to only having this one method. Refactor candidate. </summary>
public static class Standings
{
	public static IList<TeamRecord> CreateFrom(IEnumerable<Game> games)
	{
		var teams = games.SelectMany(g => new[] { g.HomeTeam, g.AwayTeam }).Where(t => t != null).Distinct();

		//TODO Make ordering configurable
		var records = teams
			.Select(team => new TeamRecord(team, games))
			.OrderByDescending(r => r.Points)
			.ThenByDescending(r => r.GoalDifference)
			.ThenByDescending(r => r.GoalsFor)
			.ThenByDescending(r => r.Wins)
			.Select((r, i) => r.SetPosition(i + 1));
		return records.ToList();
	}
}
