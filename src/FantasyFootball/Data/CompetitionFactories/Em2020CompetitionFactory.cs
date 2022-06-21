namespace FantasyFootball.Data.CompetitionFactories;

public class Em2020CompetitionFactory : DefaultEmCompetitionFactory
{
	public Em2020CompetitionFactory(IRepository repo) : base(repo) { }

	public override List<Group> CreateGroups()
	{
		return new()
		{
			new()
			{
				Name = Res.Group + " A",
				Teams = new()
				{
					Team("ITA"),
					Team("SUI"),
					Team("TUR"),
					Team("WAL"),
				},
			},
			new()
			{
				Name = Res.Group + " B",
				Teams = new()
				{
					Team("BEL"),
					Team("DEN"),
					Team("FIN"),
					Team("RUS"),
				},
			},
			new()
			{
				Name = Res.Group + " C",
				Teams = new()
				{
					Team("NED"),
					Team("MKD"),
					Team("UKR"),
					Team("AUT"),
				},
			},
			new()
			{
				Name = Res.Group + " D",
				Teams = new()
				{
					Team("ENG"),
					Team("CRO"),
					Team("SCO"),
					Team("CZE"),
				},
			},
			new()
			{
				Name = Res.Group + " E",
				Teams = new()
				{
					Team("POL"),
					Team("SWE"),
					Team("SVK"),
					Team("ESP"),
				},
			},
			new()
			{
				Name = Res.Group + " F",
				Teams = new()
				{
					Team("GER"),
					Team("FRA"),
					Team("POR"),
					Team("HUN"),
				},
			},
		};
	}

	public override List<Team> SelectParticipants() => _participantPool.ToList();

	public override Stage CreateGroupStage()
	{
		return new Stage
		{
			Name = Res.GroupStage,
			Groups = Groups,
			Rounds = new()
			{
				new()
				{
					Name = Res.Round + " 1",
					RegularGames = new()
					{
						new() { HomeTeam = Team("TUR"), AwayTeam = Team("ITA"), PlayedOn = new DateTime(2020, 6, 11, 21, 0, 0), },
						new() { HomeTeam = Team("WAL"), AwayTeam = Team("SUI"), PlayedOn = new DateTime(2020, 6, 12, 15, 0, 0), },
						new() { HomeTeam = Team("DEN"), AwayTeam = Team("FIN"), PlayedOn = new DateTime(2020, 6, 12, 18, 0, 0), },
						new() { HomeTeam = Team("BEL"), AwayTeam = Team("RUS"), PlayedOn = new DateTime(2020, 6, 12, 21, 0, 0), },
						new() { HomeTeam = Team("ENG"), AwayTeam = Team("CRO"), PlayedOn = new DateTime(2020, 6, 13, 15, 0, 0), },
						new() { HomeTeam = Team("AUT"), AwayTeam = Team("MKD"), PlayedOn = new DateTime(2020, 6, 13, 18, 0, 0), },
						new() { HomeTeam = Team("NED"), AwayTeam = Team("UKR"), PlayedOn = new DateTime(2020, 6, 13, 21, 0, 0), },
						new() { HomeTeam = Team("SCO"), AwayTeam = Team("CZE"), PlayedOn = new DateTime(2020, 6, 14, 15, 0, 0), },
						new() { HomeTeam = Team("POL"), AwayTeam = Team("SVK"), PlayedOn = new DateTime(2020, 6, 14, 18, 0, 0), },
						new() { HomeTeam = Team("ESP"), AwayTeam = Team("SWE"), PlayedOn = new DateTime(2020, 6, 14, 21, 0, 0), },
						new() { HomeTeam = Team("HUN"), AwayTeam = Team("POR"), PlayedOn = new DateTime(2020, 6, 15, 18, 0, 0), },
						new() { HomeTeam = Team("FRA"), AwayTeam = Team("GER"), PlayedOn = new DateTime(2020, 6, 15, 21, 0, 0), },
					}
				},
				new()
				{
					Name = Res.Round + " 2",
					RegularGames = new()
					{
						new() { HomeTeam = Team("FIN"), AwayTeam = Team("RUS"), PlayedOn = new DateTime(2020, 6, 16, 15, 0, 0), },
						new() { HomeTeam = Team("TUR"), AwayTeam = Team("WAL"), PlayedOn = new DateTime(2020, 6, 16, 18, 0, 0), },
						new() { HomeTeam = Team("ITA"), AwayTeam = Team("SUI"), PlayedOn = new DateTime(2020, 6, 16, 21, 0, 0), },
						new() { HomeTeam = Team("UKR"), AwayTeam = Team("MKD"), PlayedOn = new DateTime(2020, 6, 17, 15, 0, 0), },
						new() { HomeTeam = Team("DEN"), AwayTeam = Team("BEL"), PlayedOn = new DateTime(2020, 6, 17, 18, 0, 0), },
						new() { HomeTeam = Team("NED"), AwayTeam = Team("AUT"), PlayedOn = new DateTime(2020, 6, 17, 21, 0, 0), },
						new() { HomeTeam = Team("SWE"), AwayTeam = Team("SVK"), PlayedOn = new DateTime(2020, 6, 18, 15, 0, 0), },
						new() { HomeTeam = Team("CRO"), AwayTeam = Team("CZE"), PlayedOn = new DateTime(2020, 6, 18, 18, 0, 0), },
						new() { HomeTeam = Team("ENG"), AwayTeam = Team("SCO"), PlayedOn = new DateTime(2020, 6, 18, 21, 0, 0), },
						new() { HomeTeam = Team("HUN"), AwayTeam = Team("FRA"), PlayedOn = new DateTime(2020, 6, 19, 15, 0, 0), },
						new() { HomeTeam = Team("POR"), AwayTeam = Team("GER"), PlayedOn = new DateTime(2020, 6, 19, 18, 0, 0), },
						new() { HomeTeam = Team("ESP"), AwayTeam = Team("POL"), PlayedOn = new DateTime(2020, 6, 19, 21, 0, 0), },
					}
				},
				new()
				{
					Name = Res.Round + " 3",
					RegularGames = new()
					{
						new() { HomeTeam = Team("ITA"), AwayTeam = Team("WAL"), PlayedOn = new DateTime(2020, 6, 20, 18, 0, 0), },
						new() { HomeTeam = Team("SUI"), AwayTeam = Team("TUR"), PlayedOn = new DateTime(2020, 6, 20, 18, 0, 0), },
						new() { HomeTeam = Team("UKR"), AwayTeam = Team("AUT"), PlayedOn = new DateTime(2020, 6, 21, 18, 0, 0), },
						new() { HomeTeam = Team("MKD"), AwayTeam = Team("NED"), PlayedOn = new DateTime(2020, 6, 21, 18, 0, 0), },
						new() { HomeTeam = Team("FIN"), AwayTeam = Team("BEL"), PlayedOn = new DateTime(2020, 6, 21, 21, 0, 0), },
						new() { HomeTeam = Team("RUS"), AwayTeam = Team("DEN"), PlayedOn = new DateTime(2020, 6, 21, 21, 0, 0), },
						new() { HomeTeam = Team("CRO"), AwayTeam = Team("SCO"), PlayedOn = new DateTime(2020, 6, 22, 21, 0, 0), },
						new() { HomeTeam = Team("CZE"), AwayTeam = Team("ENG"), PlayedOn = new DateTime(2020, 6, 22, 21, 0, 0), },
						new() { HomeTeam = Team("SVK"), AwayTeam = Team("ESP"), PlayedOn = new DateTime(2020, 6, 23, 18, 0, 0), },
						new() { HomeTeam = Team("SWE"), AwayTeam = Team("POL"), PlayedOn = new DateTime(2020, 6, 23, 18, 0, 0), },
						new() { HomeTeam = Team("GER"), AwayTeam = Team("HUN"), PlayedOn = new DateTime(2020, 6, 23, 21, 0, 0), },
						new() { HomeTeam = Team("POR"), AwayTeam = Team("FRA"), PlayedOn = new DateTime(2020, 6, 23, 21, 0, 0), },
					}
				}
			},
		};
	}
}
