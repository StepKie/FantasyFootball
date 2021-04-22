namespace FantasyFootball.Models;

public class Standings
{
	public static IList<TeamRecord> CreateFrom(IEnumerable<Team> teams, IEnumerable<Game> games)
	{
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

	public static IList<TeamRecord> CreateFrom(IEnumerable<Game> games)
	{
		var teams = games.SelectMany(g => new[] { g.HomeTeam, g.AwayTeam }).Where(t => t != null).Distinct() as IEnumerable<Team>;
		return CreateFrom(teams, games);
	}
}
