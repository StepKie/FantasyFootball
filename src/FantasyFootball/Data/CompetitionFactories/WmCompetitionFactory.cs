namespace FantasyFootball.Data.CompetitionFactories;

public class WmCompetitionFactory : CompetitionFactory
{
	public WmCompetitionFactory(List<Group> groups) : base(CompetitionType.WM, HistoricalData.WM_2021_START, groups) { }

	public static WmCompetitionFactory Default(IDataService dataService) => new WmCompetitionFactory(dataService.CreateFromHistoricalData(CompetitionType.WM));

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
						RegularGames = new()
						{
							new() { HomeTeam = Groups[0].Teams[2], AwayTeam = Groups[0].Teams[3], PlayedOn = new(2022, 11, 21, 11, 0, 0), },
							new() { HomeTeam = Groups[1].Teams[0], AwayTeam = Groups[1].Teams[1], PlayedOn = new(2022, 11, 21, 14, 0, 0), },
							new() { HomeTeam = Groups[0].Teams[0], AwayTeam = Groups[0].Teams[1], PlayedOn = new(2022, 11, 21, 17, 0, 0), },
							new() { HomeTeam = Groups[1].Teams[2], AwayTeam = Groups[1].Teams[3], PlayedOn = new(2022, 11, 21, 20, 0, 0), },
							new() { HomeTeam = Groups[2].Teams[0], AwayTeam = Groups[2].Teams[1], PlayedOn = new(2022, 11, 22, 11, 0, 0), },
							new() { HomeTeam = Groups[3].Teams[2], AwayTeam = Groups[3].Teams[3], PlayedOn = new(2022, 11, 22, 14, 0, 0), },
							new() { HomeTeam = Groups[2].Teams[2], AwayTeam = Groups[2].Teams[3], PlayedOn = new(2022, 11, 22, 17, 0, 0), },
							new() { HomeTeam = Groups[3].Teams[0], AwayTeam = Groups[3].Teams[1], PlayedOn = new(2022, 11, 22, 20, 0, 0), },
							new() { HomeTeam = Groups[5].Teams[2], AwayTeam = Groups[5].Teams[3], PlayedOn = new(2022, 11, 23, 11, 0, 0), },
							new() { HomeTeam = Groups[4].Teams[2], AwayTeam = Groups[4].Teams[3], PlayedOn = new(2022, 11, 23, 14, 0, 0), },
							new() { HomeTeam = Groups[4].Teams[0], AwayTeam = Groups[4].Teams[1], PlayedOn = new(2022, 11, 23, 17, 0, 0), },
							new() { HomeTeam = Groups[5].Teams[0], AwayTeam = Groups[5].Teams[1], PlayedOn = new(2022, 11, 23, 20, 0, 0), },
							new() { HomeTeam = Groups[6].Teams[2], AwayTeam = Groups[6].Teams[3], PlayedOn = new(2022, 11, 24, 11, 0, 0), },
							new() { HomeTeam = Groups[7].Teams[2], AwayTeam = Groups[7].Teams[3], PlayedOn = new(2022, 11, 24, 14, 0, 0), },
							new() { HomeTeam = Groups[7].Teams[0], AwayTeam = Groups[7].Teams[1], PlayedOn = new(2022, 11, 24, 17, 0, 0), },
							new() { HomeTeam = Groups[6].Teams[0], AwayTeam = Groups[6].Teams[1], PlayedOn = new(2022, 11, 24, 20, 0, 0), },
						}
					},
					new()
					{
						Name = Res.Round + " 2",
						RegularGames = new()
						{
							new() { HomeTeam = Groups[1].Teams[3], AwayTeam = Groups[1].Teams[1], PlayedOn = new(2022, 11, 25, 11, 0, 0), },
							new() { HomeTeam = Groups[0].Teams[0], AwayTeam = Groups[0].Teams[2], PlayedOn = new(2022, 11, 25, 14, 0, 0), },
							new() { HomeTeam = Groups[0].Teams[3], AwayTeam = Groups[0].Teams[1], PlayedOn = new(2022, 11, 25, 17, 0, 0), },
							new() { HomeTeam = Groups[1].Teams[0], AwayTeam = Groups[1].Teams[2], PlayedOn = new(2022, 11, 25, 20, 0, 0), },
							new() { HomeTeam = Groups[3].Teams[3], AwayTeam = Groups[3].Teams[1], PlayedOn = new(2022, 11, 26, 11, 0, 0), },
							new() { HomeTeam = Groups[2].Teams[3], AwayTeam = Groups[2].Teams[1], PlayedOn = new(2022, 11, 26, 14, 0, 0), },
							new() { HomeTeam = Groups[3].Teams[0], AwayTeam = Groups[3].Teams[2], PlayedOn = new(2022, 11, 26, 17, 0, 0), },
							new() { HomeTeam = Groups[2].Teams[0], AwayTeam = Groups[2].Teams[2], PlayedOn = new(2022, 11, 26, 20, 0, 0), },
							new() { HomeTeam = Groups[4].Teams[3], AwayTeam = Groups[4].Teams[1], PlayedOn = new(2022, 11, 27, 11, 0, 0), },
							new() { HomeTeam = Groups[5].Teams[0], AwayTeam = Groups[5].Teams[2], PlayedOn = new(2022, 11, 27, 14, 0, 0), },
							new() { HomeTeam = Groups[5].Teams[3], AwayTeam = Groups[5].Teams[1], PlayedOn = new(2022, 11, 27, 17, 0, 0), },
							new() { HomeTeam = Groups[4].Teams[0], AwayTeam = Groups[4].Teams[2], PlayedOn = new(2022, 11, 27, 20, 0, 0), },
							new() { HomeTeam = Groups[6].Teams[3], AwayTeam = Groups[6].Teams[1], PlayedOn = new(2022, 11, 28, 11, 0, 0), },
							new() { HomeTeam = Groups[7].Teams[3], AwayTeam = Groups[7].Teams[1], PlayedOn = new(2022, 11, 28, 14, 0, 0), },
							new() { HomeTeam = Groups[6].Teams[0], AwayTeam = Groups[6].Teams[2], PlayedOn = new(2022, 11, 28, 17, 0, 0), },
							new() { HomeTeam = Groups[7].Teams[0], AwayTeam = Groups[7].Teams[2], PlayedOn = new(2022, 11, 28, 20, 0, 0), },
						}
					},
					new()
					{
						Name = Res.Round + " 3",
						RegularGames = new()
						{
							new() { HomeTeam = Groups[0].Teams[3], AwayTeam = Groups[0].Teams[0], PlayedOn = new(2022, 11, 29, 16, 0, 0), },
							new() { HomeTeam = Groups[0].Teams[1], AwayTeam = Groups[0].Teams[2], PlayedOn = new(2022, 11, 29, 16, 0, 0), },
							new() { HomeTeam = Groups[1].Teams[1], AwayTeam = Groups[1].Teams[2], PlayedOn = new(2022, 11, 29, 20, 0, 0), },
							new() { HomeTeam = Groups[1].Teams[3], AwayTeam = Groups[1].Teams[0], PlayedOn = new(2022, 11, 29, 20, 0, 0), },
							new() { HomeTeam = Groups[3].Teams[3], AwayTeam = Groups[3].Teams[0], PlayedOn = new(2022, 11, 30, 16, 0, 0), },
							new() { HomeTeam = Groups[3].Teams[1], AwayTeam = Groups[3].Teams[2], PlayedOn = new(2022, 11, 30, 16, 0, 0), },
							new() { HomeTeam = Groups[2].Teams[1], AwayTeam = Groups[2].Teams[2], PlayedOn = new(2022, 11, 30, 20, 0, 0), },
							new() { HomeTeam = Groups[2].Teams[3], AwayTeam = Groups[2].Teams[0], PlayedOn = new(2022, 11, 30, 20, 0, 0), },
							new() { HomeTeam = Groups[5].Teams[1], AwayTeam = Groups[5].Teams[2], PlayedOn = new(2022, 12, 01, 16, 0, 0), },
							new() { HomeTeam = Groups[5].Teams[3], AwayTeam = Groups[5].Teams[0], PlayedOn = new(2022, 12, 01, 16, 0, 0), },
							new() { HomeTeam = Groups[4].Teams[1], AwayTeam = Groups[4].Teams[2], PlayedOn = new(2022, 12, 01, 20, 0, 0), },
							new() { HomeTeam = Groups[4].Teams[3], AwayTeam = Groups[4].Teams[0], PlayedOn = new(2022, 12, 01, 20, 0, 0), },
							new() { HomeTeam = Groups[7].Teams[1], AwayTeam = Groups[7].Teams[2], PlayedOn = new(2022, 12, 02, 16, 0, 0), },
							new() { HomeTeam = Groups[7].Teams[3], AwayTeam = Groups[7].Teams[0], PlayedOn = new(2022, 12, 02, 16, 0, 0), },
							new() { HomeTeam = Groups[6].Teams[1], AwayTeam = Groups[6].Teams[2], PlayedOn = new(2022, 12, 02, 20, 0, 0), },
							new() { HomeTeam = Groups[6].Teams[3], AwayTeam = Groups[6].Teams[0], PlayedOn = new(2022, 12, 02, 20, 0, 0), },
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

	public Team Team(string shortName) => null;
	// public Team Team(string shortName) => AllTeams.FirstOrDefault(t => t.ShortName == shortName) ?? throw new ArgumentException($"Team {shortName} not found in db");
}
