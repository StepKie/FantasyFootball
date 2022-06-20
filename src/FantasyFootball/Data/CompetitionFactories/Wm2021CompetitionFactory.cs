namespace FantasyFootball.Data.CompetitionFactories;

public class Wm2022CompetitionFactory : CompetitionFactory
{
	public Wm2022CompetitionFactory(IRepository repo) : base(CompetitionType.WM, startDate: HistoricalData.WM_2022_START, repo) { }

	public override List<Group> CreateGroups()
	{
		return new()
		{
			new()
			{
				Name = Res.Group + " A",
				Teams = new()
				{
					Team("QAT"),
					Team("ECU"),
					Team("SEN"),
					Team("NED"),
				},
			},
			new()
			{
				Name = Res.Group + " B",
				Teams = new()
				{
					Team("ENG"),
					Team("IRN"),
					Team("USA"),
					Team("WAL"),
				},
			},
			new()
			{
				Name = Res.Group + " C",
				Teams = new()
				{
					Team("ARG"),
					Team("RSA"),
					Team("MEX"),
					Team("POL"),
				},
			},
			new()
			{
				Name = Res.Group + " D",
				Teams = new()
				{
					Team("FRA"),
					Team("AUS"),
					Team("DEN"),
					Team("TUN"),
				},
			},
			new()
			{
				Name = Res.Group + " E",
				Teams = new()
				{
					Team("ESP"),
					Team("CRC"),
					Team("GER"),
					Team("JPN"),
				},
			},
			new()
			{
				Name = Res.Group + " F",
				Teams = new()
				{
					Team("BEL"),
					Team("CAN"),
					Team("MAR"),
					Team("CRO"),
				},
			},
			new()
			{
				Name = Res.Group + " G",
				Teams = new()
				{
					Team("BRA"),
					Team("SRB"),
					Team("SUI"),
					Team("CMR"),
				},
			},
			new()
			{
				Name = Res.Group + " H",
				Teams = new()
				{
					Team("POR"),
					Team("GHA"),
					Team("URU"),
					Team("KOR"),
				},
			},
		};
	}

	public override List<Team> SelectParticipants() => _participantPool.ToList();

	public override List<Stage> CreateStages()
	{
		return new()
		{
			new Stage
			{
				Name = Res.GroupStage,
				Groups = Groups,
				Rounds = new()
				{
					new()
					{
						Name = Res.Round + " 1",
						Games = new()
						{
							new() { HomeTeam = Team("SEN"), AwayTeam = Team("NED"), PlayedOn = new(2022, 11, 21, 11, 0, 0), },
							new() { HomeTeam = Team("ENG"), AwayTeam = Team("IRN"), PlayedOn = new(2022, 11, 21, 14, 0, 0), },
							new() { HomeTeam = Team("QAT"), AwayTeam = Team("ECU"), PlayedOn = new(2022, 11, 21, 17, 0, 0), },
							new() { HomeTeam = Team("USA"), AwayTeam = Team("WAL"), PlayedOn = new(2022, 11, 21, 20, 0, 0), },
							new() { HomeTeam = Team("ARG"), AwayTeam = Team("RSA"), PlayedOn = new(2022, 11, 22, 11, 0, 0), },
							new() { HomeTeam = Team("DEN"), AwayTeam = Team("TUN"), PlayedOn = new(2022, 11, 22, 14, 0, 0), },
							new() { HomeTeam = Team("MEX"), AwayTeam = Team("POL"), PlayedOn = new(2022, 11, 22, 17, 0, 0), },
							new() { HomeTeam = Team("FRA"), AwayTeam = Team("AUS"), PlayedOn = new(2022, 11, 22, 20, 0, 0), },
							new() { HomeTeam = Team("MAR"), AwayTeam = Team("CRO"), PlayedOn = new(2022, 11, 23, 11, 0, 0), },
							new() { HomeTeam = Team("GER"), AwayTeam = Team("JPN"), PlayedOn = new(2022, 11, 23, 14, 0, 0), },
							new() { HomeTeam = Team("ESP"), AwayTeam = Team("CRC"), PlayedOn = new(2022, 11, 23, 17, 0, 0), },
							new() { HomeTeam = Team("BEL"), AwayTeam = Team("CAN"), PlayedOn = new(2022, 11, 23, 20, 0, 0), },
							new() { HomeTeam = Team("SUI"), AwayTeam = Team("CMR"), PlayedOn = new(2022, 11, 24, 11, 0, 0), },
							new() { HomeTeam = Team("URU"), AwayTeam = Team("KOR"), PlayedOn = new(2022, 11, 24, 14, 0, 0), },
							new() { HomeTeam = Team("POR"), AwayTeam = Team("GHA"), PlayedOn = new(2022, 11, 24, 17, 0, 0), },
							new() { HomeTeam = Team("BRA"), AwayTeam = Team("SRB"), PlayedOn = new(2022, 11, 24, 20, 0, 0), },
						}
					},
					new()
					{
						Name = Res.Round + " 2",
						Games = new()
						{
							new() { HomeTeam = Team("WAL"), AwayTeam = Team("IRN"), PlayedOn = new(2022, 11, 25, 11, 0, 0), },
							new() { HomeTeam = Team("QAT"), AwayTeam = Team("SEN"), PlayedOn = new(2022, 11, 25, 14, 0, 0), },
							new() { HomeTeam = Team("NED"), AwayTeam = Team("ECU"), PlayedOn = new(2022, 11, 25, 17, 0, 0), },
							new() { HomeTeam = Team("ENG"), AwayTeam = Team("USA"), PlayedOn = new(2022, 11, 25, 20, 0, 0), },
							new() { HomeTeam = Team("TUN"), AwayTeam = Team("AUS"), PlayedOn = new(2022, 11, 26, 11, 0, 0), },
							new() { HomeTeam = Team("POL"), AwayTeam = Team("RSA"), PlayedOn = new(2022, 11, 26, 14, 0, 0), },
							new() { HomeTeam = Team("FRA"), AwayTeam = Team("DEN"), PlayedOn = new(2022, 11, 26, 17, 0, 0), },
							new() { HomeTeam = Team("ARG"), AwayTeam = Team("MEX"), PlayedOn = new(2022, 11, 26, 20, 0, 0), },
							new() { HomeTeam = Team("JPN"), AwayTeam = Team("CRC"), PlayedOn = new(2022, 11, 27, 11, 0, 0), },
							new() { HomeTeam = Team("BEL"), AwayTeam = Team("MAR"), PlayedOn = new(2022, 11, 27, 14, 0, 0), },
							new() { HomeTeam = Team("CRO"), AwayTeam = Team("CAN"), PlayedOn = new(2022, 11, 27, 17, 0, 0), },
							new() { HomeTeam = Team("ESP"), AwayTeam = Team("GER"), PlayedOn = new(2022, 11, 27, 20, 0, 0), },
							new() { HomeTeam = Team("CMR"), AwayTeam = Team("SRB"), PlayedOn = new(2022, 11, 28, 11, 0, 0), },
							new() { HomeTeam = Team("KOR"), AwayTeam = Team("GHA"), PlayedOn = new(2022, 11, 28, 14, 0, 0), },
							new() { HomeTeam = Team("BRA"), AwayTeam = Team("SUI"), PlayedOn = new(2022, 11, 28, 17, 0, 0), },
							new() { HomeTeam = Team("POR"), AwayTeam = Team("URU"), PlayedOn = new(2022, 11, 28, 20, 0, 0), },
						}
					},
					new()
					{
						Name = Res.Round + " 3",
						Games = new()
						{
							new() { HomeTeam = Team("ECU"), AwayTeam = Team("SEN"), PlayedOn = new(2022, 11, 29, 16, 0, 0), },
							new() { HomeTeam = Team("NED"), AwayTeam = Team("QAT"), PlayedOn = new(2022, 11, 29, 16, 0, 0), },
							new() { HomeTeam = Team("IRN"), AwayTeam = Team("USA"), PlayedOn = new(2022, 11, 29, 20, 0, 0), },
							new() { HomeTeam = Team("WAL"), AwayTeam = Team("ENG"), PlayedOn = new(2022, 11, 29, 20, 0, 0), },
							new() { HomeTeam = Team("TUN"), AwayTeam = Team("FRA"), PlayedOn = new(2022, 11, 30, 16, 0, 0), },
							new() { HomeTeam = Team("AUS"), AwayTeam = Team("DEN"), PlayedOn = new(2022, 11, 30, 16, 0, 0), },
							new() { HomeTeam = Team("POL"), AwayTeam = Team("ARG"), PlayedOn = new(2022, 11, 30, 20, 0, 0), },
							new() { HomeTeam = Team("RSA"), AwayTeam = Team("MEX"), PlayedOn = new(2022, 11, 30, 20, 0, 0), },
							new() { HomeTeam = Team("CRO"), AwayTeam = Team("BEL"), PlayedOn = new(2022, 12, 01, 16, 0, 0), },
							new() { HomeTeam = Team("CAN"), AwayTeam = Team("MAR"), PlayedOn = new(2022, 12, 01, 16, 0, 0), },
							new() { HomeTeam = Team("JPN"), AwayTeam = Team("ESP"), PlayedOn = new(2022, 12, 01, 20, 0, 0), },
							new() { HomeTeam = Team("CRC"), AwayTeam = Team("GER"), PlayedOn = new(2022, 12, 01, 20, 0, 0), },
							new() { HomeTeam = Team("KOR"), AwayTeam = Team("POR"), PlayedOn = new(2022, 12, 02, 16, 0, 0), },
							new() { HomeTeam = Team("GHA"), AwayTeam = Team("URU"), PlayedOn = new(2022, 12, 02, 16, 0, 0), },
							new() { HomeTeam = Team("SRB"), AwayTeam = Team("SUI"), PlayedOn = new(2022, 12, 02, 20, 0, 0), },
							new() { HomeTeam = Team("CMR"), AwayTeam = Team("BRA"), PlayedOn = new(2022, 12, 02, 20, 0, 0), },
						}
					}
				},
			},
			new Stage
			{
				Name = Res.KoStage,
				Rounds = new()
				{
					new()
					{
						Name = Res.RoundOf16,
						KoGames = new()
						{
							// Order is not chronological since UEFA is weird
							new(49, Qualifier.FromGroup("A1"), Qualifier.FromGroup("B2"), new(2022, 12, 03, 16, 0, 0)),
							new(50, Qualifier.FromGroup("C1"), Qualifier.FromGroup("D2"), new(2022, 12, 03, 20, 0, 0)),
							new(51, Qualifier.FromGroup("D1"), Qualifier.FromGroup("C2"), new(2022, 12, 04, 16, 0, 0)),
							new(52, Qualifier.FromGroup("B1"), Qualifier.FromGroup("A2"), new(2022, 12, 04, 20, 0, 0)),
							new(53, Qualifier.FromGroup("E1"), Qualifier.FromGroup("F2"), new(2022, 12, 05, 16, 0, 0)),
							new(54, Qualifier.FromGroup("G1"), Qualifier.FromGroup("H2"), new(2022, 12, 05, 20, 0, 0)),
							new(55, Qualifier.FromGroup("F1"), Qualifier.FromGroup("E2"), new(2022, 12, 06, 16, 0, 0)),
							new(56, Qualifier.FromGroup("H1"), Qualifier.FromGroup("G2"), new(2022, 12, 06, 20, 0, 0)),
						}
					},
					new()
					{
						Name = Res.Quarterfinal,
						KoGames = new()
						{
							new(57, Qualifier.FromGame(53), Qualifier.FromGame(54), new(2022, 12, 09, 16, 0, 0)),
							new(58, Qualifier.FromGame(49), Qualifier.FromGame(50), new(2022, 12, 09, 20, 0, 0)),
							new(59, Qualifier.FromGame(55), Qualifier.FromGame(56), new(2022, 12, 10, 16, 0, 0)),
							new(60, Qualifier.FromGame(51), Qualifier.FromGame(52), new(2022, 12, 10, 20, 0, 0)),
						}
					},
					new()
					{
						Name = Res.Semifinal,
						KoGames = new()
						{
							new(61, Qualifier.FromGame(57), Qualifier.FromGame(58), new(2022, 12, 13, 16, 0, 0)),
							new(62, Qualifier.FromGame(59), Qualifier.FromGame(60), new(2022, 12, 14, 16, 0, 0)),
						}
					},
					new()
					{
						Name = Res.ThirdPlaceMatch,
						KoGames = new()
						{
							new(63, Qualifier.FromGame(61, loserQualifies: true), Qualifier.FromGame(62, loserQualifies: true), new(2022, 12, 17, 16, 0, 0)),
						},
					},
					new()
					{
						Name = Res.Final,
						KoGames = new()
						{
							new(64, Qualifier.FromGame(61), Qualifier.FromGame(62), new(2022, 12, 18, 16, 0, 0)),
						},
					}
				}
			}
		};
	}
}
