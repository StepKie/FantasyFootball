namespace FantasyFootball.Data.CompetitionFactories;

public class DefaultEmCompetitionFactory : DefaultTournamentFactory
{
	public DefaultEmCompetitionFactory(IRepository repo) : base(CompetitionType.EM, startDate: HistoricalData.EM_2020_START, noOfGroups: 6, groupSize: 4, repo) { }

	public override List<Team> SelectParticipants() => new TeamSelector(_participantPool).DrawTeamsWeightedByElo(24, Confederation.UEFA);

	public override List<Stage> CreateStages()
	{
		return new()
		{
			CreateGroupStage(),
			CreateKoStage(),
		};
	}

	public virtual Stage CreateGroupStage()
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
					Games = new()
					{
						new() { HomeTeam = Groups[0].Teams[0], AwayTeam = Groups[0].Teams[2], PlayedOn = new DateTime(2020, 6, 11, 21, 0, 0), },
						new() { HomeTeam = Groups[0].Teams[1], AwayTeam = Groups[0].Teams[3], PlayedOn = new DateTime(2020, 6, 12, 15, 0, 0), },
						new() { HomeTeam = Groups[1].Teams[0], AwayTeam = Groups[1].Teams[2], PlayedOn = new DateTime(2020, 6, 12, 18, 0, 0), },
						new() { HomeTeam = Groups[1].Teams[1], AwayTeam = Groups[1].Teams[3], PlayedOn = new DateTime(2020, 6, 12, 21, 0, 0), },
						new() { HomeTeam = Groups[2].Teams[0], AwayTeam = Groups[2].Teams[2], PlayedOn = new DateTime(2020, 6, 13, 15, 0, 0), },
						new() { HomeTeam = Groups[2].Teams[1], AwayTeam = Groups[2].Teams[3], PlayedOn = new DateTime(2020, 6, 13, 18, 0, 0), },
						new() { HomeTeam = Groups[3].Teams[0], AwayTeam = Groups[3].Teams[2], PlayedOn = new DateTime(2020, 6, 13, 21, 0, 0), },
						new() { HomeTeam = Groups[3].Teams[1], AwayTeam = Groups[3].Teams[3], PlayedOn = new DateTime(2020, 6, 14, 15, 0, 0), },
						new() { HomeTeam = Groups[4].Teams[0], AwayTeam = Groups[4].Teams[2], PlayedOn = new DateTime(2020, 6, 14, 18, 0, 0), },
						new() { HomeTeam = Groups[4].Teams[1], AwayTeam = Groups[4].Teams[3], PlayedOn = new DateTime(2020, 6, 14, 21, 0, 0), },
						new() { HomeTeam = Groups[5].Teams[0], AwayTeam = Groups[5].Teams[2], PlayedOn = new DateTime(2020, 6, 15, 18, 0, 0), },
						new() { HomeTeam = Groups[5].Teams[1], AwayTeam = Groups[5].Teams[3], PlayedOn = new DateTime(2020, 6, 15, 21, 0, 0), },
					}
				},
				new()
				{
					Name = Res.Round + " 2",
					Games = new()
					{
						new() { HomeTeam = Groups[0].Teams[0], AwayTeam = Groups[0].Teams[3], PlayedOn = new DateTime(2020, 6, 16, 15, 0, 0), },
						new() { HomeTeam = Groups[0].Teams[1], AwayTeam = Groups[0].Teams[2], PlayedOn = new DateTime(2020, 6, 16, 18, 0, 0), },
						new() { HomeTeam = Groups[1].Teams[0], AwayTeam = Groups[1].Teams[3], PlayedOn = new DateTime(2020, 6, 16, 21, 0, 0), },
						new() { HomeTeam = Groups[1].Teams[1], AwayTeam = Groups[1].Teams[2], PlayedOn = new DateTime(2020, 6, 17, 15, 0, 0), },
						new() { HomeTeam = Groups[2].Teams[0], AwayTeam = Groups[2].Teams[3], PlayedOn = new DateTime(2020, 6, 17, 18, 0, 0), },
						new() { HomeTeam = Groups[2].Teams[1], AwayTeam = Groups[2].Teams[2], PlayedOn = new DateTime(2020, 6, 17, 21, 0, 0), },
						new() { HomeTeam = Groups[3].Teams[0], AwayTeam = Groups[3].Teams[3], PlayedOn = new DateTime(2020, 6, 18, 15, 0, 0), },
						new() { HomeTeam = Groups[3].Teams[1], AwayTeam = Groups[3].Teams[2], PlayedOn = new DateTime(2020, 6, 18, 18, 0, 0), },
						new() { HomeTeam = Groups[4].Teams[0], AwayTeam = Groups[4].Teams[3], PlayedOn = new DateTime(2020, 6, 18, 21, 0, 0), },
						new() { HomeTeam = Groups[4].Teams[1], AwayTeam = Groups[4].Teams[2], PlayedOn = new DateTime(2020, 6, 19, 15, 0, 0), },
						new() { HomeTeam = Groups[5].Teams[0], AwayTeam = Groups[5].Teams[3], PlayedOn = new DateTime(2020, 6, 19, 18, 0, 0), },
						new() { HomeTeam = Groups[5].Teams[1], AwayTeam = Groups[5].Teams[2], PlayedOn = new DateTime(2020, 6, 19, 21, 0, 0), },
					}
				},
				new()
				{
					Name = Res.Round + " 3",
					Games = new()
					{
						new() { HomeTeam = Groups[0].Teams[0], AwayTeam = Groups[0].Teams[1], PlayedOn = new DateTime(2020, 6, 20, 18, 0, 0), },
						new() { HomeTeam = Groups[0].Teams[2], AwayTeam = Groups[0].Teams[3], PlayedOn = new DateTime(2020, 6, 20, 18, 0, 0), },
						new() { HomeTeam = Groups[1].Teams[0], AwayTeam = Groups[1].Teams[1], PlayedOn = new DateTime(2020, 6, 21, 18, 0, 0), },
						new() { HomeTeam = Groups[1].Teams[2], AwayTeam = Groups[1].Teams[3], PlayedOn = new DateTime(2020, 6, 21, 18, 0, 0), },
						new() { HomeTeam = Groups[2].Teams[0], AwayTeam = Groups[2].Teams[1], PlayedOn = new DateTime(2020, 6, 21, 21, 0, 0), },
						new() { HomeTeam = Groups[2].Teams[2], AwayTeam = Groups[2].Teams[3], PlayedOn = new DateTime(2020, 6, 21, 21, 0, 0), },
						new() { HomeTeam = Groups[3].Teams[0], AwayTeam = Groups[3].Teams[1], PlayedOn = new DateTime(2020, 6, 22, 21, 0, 0), },
						new() { HomeTeam = Groups[3].Teams[2], AwayTeam = Groups[3].Teams[3], PlayedOn = new DateTime(2020, 6, 22, 21, 0, 0), },
						new() { HomeTeam = Groups[4].Teams[0], AwayTeam = Groups[4].Teams[1], PlayedOn = new DateTime(2020, 6, 23, 18, 0, 0), },
						new() { HomeTeam = Groups[4].Teams[2], AwayTeam = Groups[4].Teams[3], PlayedOn = new DateTime(2020, 6, 23, 18, 0, 0), },
						new() { HomeTeam = Groups[5].Teams[0], AwayTeam = Groups[5].Teams[1], PlayedOn = new DateTime(2020, 6, 23, 21, 0, 0), },
						new() { HomeTeam = Groups[5].Teams[2], AwayTeam = Groups[5].Teams[3], PlayedOn = new DateTime(2020, 6, 23, 21, 0, 0), },
					}
				}
			},
		};
	}

	public virtual Stage CreateKoStage()
	{
		return new Stage
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
						new KoGame(37, Qualifier.FromGroup("A1"), Qualifier.FromGroup("C2"),  new(2020, 6, 26, 21, 0, 0)),
						new KoGame(38, Qualifier.FromGroup("A2"), Qualifier.FromGroup("B2"),  new(2020, 6, 26, 18, 0, 0)),
						new KoGame(39, Qualifier.FromGroup("B1"), Qualifier.ThirdPlace("A/D/E/F"), new(2020, 6, 27, 21, 0, 0)),
						new KoGame(40, Qualifier.FromGroup("C1"), Qualifier.ThirdPlace("D/E/F"), new(2020, 6, 27, 18, 0, 0)),
						new KoGame(41, Qualifier.FromGroup("F1"), Qualifier.ThirdPlace("A/B/C"), new(2020, 6, 28, 21, 0, 0)),
						new KoGame(42, Qualifier.FromGroup("D2"), Qualifier.FromGroup("E2"), new(2020, 6, 28, 18, 0, 0)),
						new KoGame(43, Qualifier.FromGroup("E1"), Qualifier.ThirdPlace("A/B/C/D"), new(2020, 6, 29, 21, 0, 0)),
						new KoGame(44, Qualifier.FromGroup("D1"), Qualifier.FromGroup("F2"), new(2020, 6, 29, 18, 0, 0)),
					}
				},
				new()
				{
					Name = Res.Quarterfinal,
					Games = new()
					{
						new KoGame(45, Qualifier.FromGame(41), Qualifier.FromGame(42), new(2020, 7, 2, 18, 0, 0)),
						new KoGame(46, Qualifier.FromGame(39), Qualifier.FromGame(37), new(2020, 7, 2, 21, 0, 0)),
						new KoGame(47, Qualifier.FromGame(40), Qualifier.FromGame(38), new(2020, 7, 3, 18, 0, 0)),
						new KoGame(48, Qualifier.FromGame(43), Qualifier.FromGame(44), new(2020, 7, 3, 21, 0, 0)),
					}
				},
				new()
				{
					Name = Res.Semifinal,
					Games = new()
					{
						new KoGame(49, Qualifier.FromGame(46), Qualifier.FromGame(45), new(2020, 7, 6, 21, 0, 0)),
						new KoGame(50, Qualifier.FromGame(48), Qualifier.FromGame(47), new(2020, 7, 7, 21, 0, 0)),
					}
				},
				new()
				{
					Name = Res.Final,
					Games = new()
					{
						new KoGame(51, Qualifier.FromGame(49), Qualifier.FromGame(50), new(2020, 7, 11, 21, 0, 0)),
					},
				}
			}
		};
	}
}
