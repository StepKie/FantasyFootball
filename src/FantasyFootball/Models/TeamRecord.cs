namespace FantasyFootball.Models;

/// <summary>
/// TeamRecords can be within a League, a Group, or over a Timespan of years, filtered on Competitions ...
/// </summary>
[Table(nameof(TeamRecord))]
public class TeamRecord : NamedUniqueId, IComparable<TeamRecord>
{
	[Ignore]
	public Team Team { get; set; }

	public int Position { get; private set; }
	public int Wins { get; set; }
	public int Losses { get; set; }
	public int Draws { get; set; }
	[Ignore] public int Points => (3 * Wins) + Draws;
	public int GoalsFor { get; set; }
	public int GoalsAgainst { get; set; }

	[Ignore]
	public int GoalDifference => GoalsFor - GoalsAgainst;

	[Ignore]
	public int MatchesPlayed => Wins + Draws + Losses;

	[Ignore]
	public double AveragePointsPerGame => (double)Points / MatchesPlayed;

	[Ignore]
	public double AverageGoalDifferencePerGame => (double)GoalDifference / MatchesPlayed;

	[Ignore]
	public int CompetitionWins { get; set; }

	public int StandingsId { get; set; }

	[ManyToOne]
	public Standings Standings { get; set; }

	public TeamRecord(Team team, IEnumerable<Game> games)
	{
		Team = team;
		var relevantGames = games.Where(game => game.IsFinished && (team.Equals(game.HomeTeam) || team.Equals(game.AwayTeam)));
		foreach (var game in relevantGames)
		{
			var teamScore = team.Equals(game.HomeTeam) ? game.HomeScore : game.AwayScore;
			var otherScore = team.Equals(game.HomeTeam) ? game.AwayScore : game.HomeScore;

			GoalsFor += teamScore;
			GoalsAgainst += otherScore;

			if (teamScore == otherScore)
			{
				Draws++;
			}
			else if (teamScore > otherScore)
			{
				Wins++;
			}
			else
			{
				Losses++;
			}
			//TODO Make more robust instead of comparing Name ...
			if (game.Round.Name == Res.Final && team.Equals(game.Winner))
			{
				CompetitionWins++;
			}
		}
	}

	public TeamRecord SetPosition(int position)
	{
		Position = position;
		return this;
	}

	public int CompareTo(TeamRecord? other) =>
		other is null ? -1 :
		Points.NullableCompareTo(other.Points)
		?? GoalDifference.NullableCompareTo(other.GoalDifference)
		?? GoalsFor.NullableCompareTo(other.GoalsFor)
		?? Wins.NullableCompareTo(other.Wins)
		?? 0;
}
