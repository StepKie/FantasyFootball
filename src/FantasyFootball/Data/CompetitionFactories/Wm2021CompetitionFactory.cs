namespace FantasyFootball.Data.CompetitionFactories;

public class Wm2021CompetitionFactory : CompetitionFactory
{
	public Wm2021CompetitionFactory(IRepository repo) : base(CompetitionType.WM, startDate: HistoricalData.WM_2021_START, repo) { }

	protected override List<Group> CreateGroups()
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

	protected override List<Team> SelectParticipants() => _participantPool.ToList();

	protected override List<Stage> CreateStages()
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
							new() { HomeTeam = Team("SEN"), AwayTeam = Team("NED"), PlayedOn = new DateTime(2022, 11, 21, 11, 0, 0), },
							new() { HomeTeam = Team("ENG"), AwayTeam = Team("IRN"), PlayedOn = new DateTime(2022, 11, 21, 14, 0, 0), },
							new() { HomeTeam = Team("QAT"), AwayTeam = Team("ECU"), PlayedOn = new DateTime(2022, 11, 21, 17, 0, 0), },
							new() { HomeTeam = Team("USA"), AwayTeam = Team("WAL"), PlayedOn = new DateTime(2022, 11, 21, 20, 0, 0), },
							new() { HomeTeam = Team("ARG"), AwayTeam = Team("RSA"), PlayedOn = new DateTime(2022, 11, 22, 11, 0, 0), },
							new() { HomeTeam = Team("DEN"), AwayTeam = Team("TUN"), PlayedOn = new DateTime(2022, 11, 22, 14, 0, 0), },
							new() { HomeTeam = Team("MEX"), AwayTeam = Team("POL"), PlayedOn = new DateTime(2022, 11, 22, 17, 0, 0), },
							new() { HomeTeam = Team("FRA"), AwayTeam = Team("AUS"), PlayedOn = new DateTime(2022, 11, 22, 20, 0, 0), },
							new() { HomeTeam = Team("MAR"), AwayTeam = Team("CRO"), PlayedOn = new DateTime(2022, 11, 23, 11, 0, 0), },
							new() { HomeTeam = Team("GER"), AwayTeam = Team("JPN"), PlayedOn = new DateTime(2022, 11, 23, 14, 0, 0), },
							new() { HomeTeam = Team("ESP"), AwayTeam = Team("CRC"), PlayedOn = new DateTime(2022, 11, 23, 17, 0, 0), },
							new() { HomeTeam = Team("BEL"), AwayTeam = Team("CAN"), PlayedOn = new DateTime(2022, 11, 23, 20, 0, 0), },
							new() { HomeTeam = Team("SUI"), AwayTeam = Team("CMR"), PlayedOn = new DateTime(2022, 11, 24, 11, 0, 0), },
							new() { HomeTeam = Team("URU"), AwayTeam = Team("KOR"), PlayedOn = new DateTime(2022, 11, 24, 14, 0, 0), },
							new() { HomeTeam = Team("POR"), AwayTeam = Team("GHA"), PlayedOn = new DateTime(2022, 11, 24, 17, 0, 0), },
							new() { HomeTeam = Team("BRA"), AwayTeam = Team("SRB"), PlayedOn = new DateTime(2022, 11, 24, 20, 0, 0), },
						}
					},
					new()
					{
						Name = Res.Round + " 2",
						Games = new()
						{
							new() { HomeTeam = Team("WAL"), AwayTeam = Team("IRN"), PlayedOn = new DateTime(2022, 11, 25, 11, 0, 0), },
							new() { HomeTeam = Team("QAT"), AwayTeam = Team("SEN"), PlayedOn = new DateTime(2022, 11, 25, 14, 0, 0), },
							new() { HomeTeam = Team("NED"), AwayTeam = Team("ECU"), PlayedOn = new DateTime(2022, 11, 25, 17, 0, 0), },
							new() { HomeTeam = Team("ENG"), AwayTeam = Team("USA"), PlayedOn = new DateTime(2022, 11, 25, 20, 0, 0), },
							new() { HomeTeam = Team("TUN"), AwayTeam = Team("AUS"), PlayedOn = new DateTime(2022, 11, 26, 11, 0, 0), },
							new() { HomeTeam = Team("POL"), AwayTeam = Team("RSA"), PlayedOn = new DateTime(2022, 11, 26, 14, 0, 0), },
							new() { HomeTeam = Team("FRA"), AwayTeam = Team("DEN"), PlayedOn = new DateTime(2022, 11, 26, 17, 0, 0), },
							new() { HomeTeam = Team("ARG"), AwayTeam = Team("MEX"), PlayedOn = new DateTime(2022, 11, 26, 20, 0, 0), },
							new() { HomeTeam = Team("JPN"), AwayTeam = Team("CRC"), PlayedOn = new DateTime(2022, 11, 27, 11, 0, 0), },
							new() { HomeTeam = Team("BEL"), AwayTeam = Team("MAR"), PlayedOn = new DateTime(2022, 11, 27, 14, 0, 0), },
							new() { HomeTeam = Team("CRO"), AwayTeam = Team("CAN"), PlayedOn = new DateTime(2022, 11, 27, 17, 0, 0), },
							new() { HomeTeam = Team("ESP"), AwayTeam = Team("GER"), PlayedOn = new DateTime(2022, 11, 27, 20, 0, 0), },
							new() { HomeTeam = Team("CMR"), AwayTeam = Team("SRB"), PlayedOn = new DateTime(2022, 11, 28, 11, 0, 0), },
							new() { HomeTeam = Team("KOR"), AwayTeam = Team("GHA"), PlayedOn = new DateTime(2022, 11, 28, 14, 0, 0), },
							new() { HomeTeam = Team("BRA"), AwayTeam = Team("SUI"), PlayedOn = new DateTime(2022, 11, 28, 17, 0, 0), },
							new() { HomeTeam = Team("POR"), AwayTeam = Team("URU"), PlayedOn = new DateTime(2022, 11, 28, 20, 0, 0), },
						}
					},
					new()
					{
						Name = Res.Round + " 3",
						Games = new()
						{
							new() { HomeTeam = Team("ECU"), AwayTeam = Team("SEN"), PlayedOn = new DateTime(2022, 11, 29, 16, 0, 0), },
							new() { HomeTeam = Team("NED"), AwayTeam = Team("QAT"), PlayedOn = new DateTime(2022, 11, 29, 16, 0, 0), },
							new() { HomeTeam = Team("IRN"), AwayTeam = Team("USA"), PlayedOn = new DateTime(2022, 11, 29, 20, 0, 0), },
							new() { HomeTeam = Team("WAL"), AwayTeam = Team("ENG"), PlayedOn = new DateTime(2022, 11, 29, 20, 0, 0), },
							new() { HomeTeam = Team("TUN"), AwayTeam = Team("FRA"), PlayedOn = new DateTime(2022, 11, 30, 16, 0, 0), },
							new() { HomeTeam = Team("AUS"), AwayTeam = Team("DEN"), PlayedOn = new DateTime(2022, 11, 30, 16, 0, 0), },
							new() { HomeTeam = Team("POL"), AwayTeam = Team("ARG"), PlayedOn = new DateTime(2022, 11, 30, 20, 0, 0), },
							new() { HomeTeam = Team("RSA"), AwayTeam = Team("MEX"), PlayedOn = new DateTime(2022, 11, 30, 20, 0, 0), },
							new() { HomeTeam = Team("CRO"), AwayTeam = Team("BEL"), PlayedOn = new DateTime(2022, 12, 01, 16, 0, 0), },
							new() { HomeTeam = Team("CAN"), AwayTeam = Team("MAR"), PlayedOn = new DateTime(2022, 12, 01, 16, 0, 0), },
							new() { HomeTeam = Team("JPN"), AwayTeam = Team("ESP"), PlayedOn = new DateTime(2022, 12, 01, 20, 0, 0), },
							new() { HomeTeam = Team("CRC"), AwayTeam = Team("GER"), PlayedOn = new DateTime(2022, 12, 01, 20, 0, 0), },
							new() { HomeTeam = Team("KOR"), AwayTeam = Team("POR"), PlayedOn = new DateTime(2022, 12, 02, 16, 0, 0), },
							new() { HomeTeam = Team("GHA"), AwayTeam = Team("URU"), PlayedOn = new DateTime(2022, 12, 02, 16, 0, 0), },
							new() { HomeTeam = Team("SRB"), AwayTeam = Team("SUI"), PlayedOn = new DateTime(2022, 12, 02, 20, 0, 0), },
							new() { HomeTeam = Team("CMR"), AwayTeam = Team("BRA"), PlayedOn = new DateTime(2022, 12, 02, 20, 0, 0), },
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
						Games = new()
						{
							// Order is not chronological since UEFA is weird
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe A", AwayTeamTentative = "2. Gruppe B", PlayedOn = new DateTime(2020, 12, 03, 16, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe C", AwayTeamTentative = "2. Gruppe D", PlayedOn = new DateTime(2020, 12, 03, 20, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe D", AwayTeamTentative = "2. Gruppe C", PlayedOn = new DateTime(2020, 12, 04, 16, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe B", AwayTeamTentative = "2. Gruppe A", PlayedOn = new DateTime(2020, 12, 04, 20, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe E", AwayTeamTentative = "2. Gruppe F", PlayedOn = new DateTime(2020, 12, 05, 16, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe G", AwayTeamTentative = "2. Gruppe H", PlayedOn = new DateTime(2020, 12, 05, 20, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe F", AwayTeamTentative = "2. Gruppe E", PlayedOn = new DateTime(2020, 12, 06, 16, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe H", AwayTeamTentative = "2. Gruppe G", PlayedOn = new DateTime(2020, 12, 06, 20, 0, 0), },
						}
					},
					new()
					{
						Name = Res.Quarterfinal,
						Games = new()
						{
							new() { IsKo = true, HomeTeamTentative = "Sieger Achtelfinale 5", AwayTeamTentative = "Sieger Achtelfinale 6", PlayedOn = new DateTime(2020, 12, 09, 16, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "Sieger Achtelfinale 1", AwayTeamTentative = "Sieger Achtelfinale 2", PlayedOn = new DateTime(2020, 12, 09, 20, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "Sieger Achtelfinale 7", AwayTeamTentative = "Sieger Achtelfinale 8", PlayedOn = new DateTime(2020, 12, 10, 16, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "Sieger Achtelfinale 3", AwayTeamTentative = "Sieger Achtelfinale 4", PlayedOn = new DateTime(2020, 12, 10, 20, 0, 0), },
						}
					},
					new()
					{
						Name = Res.Semifinal,
						Games = new()
						{
							new() { IsKo = true, HomeTeamTentative = "Sieger Viertelfinale 1", AwayTeamTentative = "Sieger Viertelfinale 2", PlayedOn = new DateTime(2020, 12, 13, 16, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "Sieger Viertelfinale 3", AwayTeamTentative = "Sieger Viertelfinale 4", PlayedOn = new DateTime(2020, 12, 14, 16, 0, 0), },

						}
					},
					new()
					{
						Name = Res.ThirdPlaceMatch,
						Games = new()
						{
							new() { IsKo = true, HomeTeamTentative = "Verlierer Halbfinale 1", AwayTeamTentative = "Verlierer Halbfinale 2", PlayedOn = new DateTime(2020, 12, 17, 16, 0, 0), },
						},
					},
					new()
					{
						Name = Res.Final,
						Games = new()
						{
							new() { IsKo = true, HomeTeamTentative = "Sieger Halbfinale 1", AwayTeamTentative = "Sieger Halbfinale 2", PlayedOn = new DateTime(2020, 12, 18, 16, 0, 0), },
						},
					}
				}
			}
		};
	}

	// TODO Replace all the Home/AwayTeamTentatives, pass in this Func
	// This can eliminate all the manual RoundAdvancer code, just use a backing field in Team.HomeTeam/Team.AwayTeam, and return _homeTeam ?? GetQualifier(...).Invoke() ?? new Team("Placeholder", isVirtual: true)
	public Func<Competition, Team> GetQualifier(string roundName, int gameNr, string standin)
	{
		return c => new Models.Team();
	}
}
