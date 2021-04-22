namespace FantasyFootball.Data;

public class MatchFactory
{
	//TODO Hardcoded WM/EM groups with 4 participants, make configurable
	public IList<Round> FromParticipants(IList<Team> participants)
	{

		var teamsByElo = participants.OrderByDescending(team => team.Elo).ToList();
		var startDate = DateTime.Now;
		IList<Round> rounds = new Round[]
		{
			new()
			{
				Name = Res.Round + "1",
				Games = new()
				{
					new() { PlayedOn = startDate, HomeTeam = teamsByElo[0], AwayTeam = teamsByElo[1] },
					new() { PlayedOn = startDate, HomeTeam = teamsByElo[2], AwayTeam = teamsByElo[3] },
				}
			},
			new()
			{
				Name = Res.Round + "2",
				Games = new()
				{
					new() { PlayedOn = startDate, HomeTeam = teamsByElo[0], AwayTeam = teamsByElo[2] },
					new() { PlayedOn = startDate, HomeTeam = teamsByElo[1], AwayTeam = teamsByElo[3] },
				}
			},
			new()
			{
				Name = Res.Round + "3",
				Games = new()
				{
					new() { PlayedOn = startDate, HomeTeam = teamsByElo[0], AwayTeam = teamsByElo[3] },
					new() { PlayedOn = startDate, HomeTeam = teamsByElo[1], AwayTeam = teamsByElo[2] },
				}
			},
		};

		return rounds;
	}
}
