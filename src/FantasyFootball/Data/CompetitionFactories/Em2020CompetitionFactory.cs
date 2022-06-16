namespace FantasyFootball.Data.CompetitionFactories;

public class Em2020CompetitionFactory : CompetitionFactory
{
	public Em2020CompetitionFactory(IRepository repo) : base(CompetitionType.EM, startDate: HistoricalData.EM_2020_START, repo) { }

	protected override List<Group> CreateGroups()
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
						Games = new()
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
						Games = new()
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
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe B", AwayTeamTentative = "3. Gruppe A/D/E/F", PlayedOn = new DateTime(2020, 6, 27, 21, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe A", AwayTeamTentative = "2. Gruppe C"      , PlayedOn = new DateTime(2020, 6, 26, 21, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe F", AwayTeamTentative = "3. Gruppe A/B/C"  , PlayedOn = new DateTime(2020, 6, 28, 21, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "2. Gruppe D", AwayTeamTentative = "2. Gruppe E"      , PlayedOn = new DateTime(2020, 6, 28, 18, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe E", AwayTeamTentative = "3. Gruppe A/B/C/D", PlayedOn = new DateTime(2020, 6, 29, 21, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe D", AwayTeamTentative = "2. Gruppe F"      , PlayedOn = new DateTime(2020, 6, 29, 18, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe C", AwayTeamTentative = "3. Gruppe D/E/F"  , PlayedOn = new DateTime(2020, 6, 27, 18, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "2. Gruppe A", AwayTeamTentative = "2. Gruppe B"      , PlayedOn = new DateTime(2020, 6, 26, 18, 0, 0), },
						}
					},
					new()
					{
						Name = Res.Quarterfinal,
						Games = new()
						{
							new() { IsKo = true, HomeTeamTentative = "Viertelfinalist 1", AwayTeamTentative = "Viertelfinalist 2", PlayedOn = new DateTime(2020, 7, 2, 18, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "Viertelfinalist 3", AwayTeamTentative = "Viertelfinalist 4", PlayedOn = new DateTime(2020, 7, 2, 21, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "Viertelfinalist 5", AwayTeamTentative = "Viertelfinalist 6", PlayedOn = new DateTime(2020, 7, 3, 18, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "Viertelfinalist 7", AwayTeamTentative = "Viertelfinalist 8", PlayedOn = new DateTime(2020, 7, 3, 21, 0, 0), },
						}
					},
					new()
					{
						Name = Res.Semifinal,
						Games = new()
						{
							new() { IsKo = true, HomeTeamTentative = "Halbfinalist 1", AwayTeamTentative = "Halbfinalist 2", PlayedOn = new DateTime(2020, 7, 6, 21, 0, 0), },
							new() { IsKo = true, HomeTeamTentative = "Halbfinalist 3", AwayTeamTentative = "Halbfinalist 4", PlayedOn = new DateTime(2020, 7, 7, 21, 0, 0), },

						}
					},
					new()
					{
						Name = Res.Final,
						Games = new()
						{
							new() { IsKo = true, HomeTeamTentative = "Finalist 1", AwayTeamTentative = "Finalist 2", PlayedOn = new DateTime(2020, 7, 11, 21, 0, 0), },
						},
					}
				}
			}
		};
	}
}
