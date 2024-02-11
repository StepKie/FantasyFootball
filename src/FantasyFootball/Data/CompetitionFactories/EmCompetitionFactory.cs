namespace FantasyFootball.Data.CompetitionFactories;

public class EmCompetitionFactory(DateTime startDate, List<Group> groups) : CompetitionFactory(CompetitionType.EM, startDate, groups)
{
	public static EmCompetitionFactory Default(IDataService dataService, int year) => new(CompetitionType.EM.StartDate(year), GroupFactory.For(dataService, CompetitionType.EM).CreateFromHistoricalData(year));
	public override List<Stage> CreateStages() => [CreateGroupStage(), CreateKoStage()];

	public virtual Stage CreateGroupStage()
	{
		return new Stage
		{
			Name = Res.GroupStage,
			Groups = Groups,
			Rounds =
			[
				new()
				{
					Name = Res.Round + " 1",
					RegularGames =
					[
						Create("A1 - A2", 0, 21),
						Create("A3 - A4", 1, 15),
						Create("B1 - B2", 1, 18),
						Create("B3 - B4", 1, 21),
						Create("D1 - D2", 2, 15),
						Create("C1 - C2", 2, 18),
						Create("C3 - C4", 2, 21),
						Create("E1 - E2", 3, 15),
						Create("E3 - E4", 3, 18),
						Create("D3 - D4", 3, 21),
						Create("F1 - F2", 4, 18),
						Create("F3 - F4", 4, 21),
					]
				},
				new()
				{
					Name = Res.Round + " 2",
					RegularGames =
					[
						Create("B2 - B4", 5, 15),
						Create("A1 - A3", 5, 18),
						Create("A2 - A4", 5, 21),
						Create("C1 - C3", 6, 15),
						Create("C2 - C4", 6, 18),
						Create("B1 - B3", 6, 21),
						Create("E2 - E4", 7, 15),
						Create("D1 - D3", 7, 18),
						Create("D2 - D4", 7, 21),
						Create("F2 - F4", 8, 15),
						Create("F1 - F3", 8, 18),
						Create("E1 - E3", 8, 21),
					]
				},
				new()
				{
					Name = Res.Round + " 3",
					RegularGames =
					[
						Create("A4 - A1", 9, 21),
						Create("A2 - A3", 9, 21),
						Create("B4 - B1", 10, 21),
						Create("B2 - B3", 10, 21),
						Create("D4 - D1", 11, 18),
						Create("D2 - D3", 11, 18),
						Create("C4 - C1", 11, 21),
						Create("C2 - C3", 11, 21),
						Create("E4 - E1", 12, 18),
						Create("E2 - E3", 12, 18),
						Create("F4 - F1", 12, 21),
						Create("F2 - F3", 12, 21),
					]
				}
			],
		};
	}

	public virtual Stage CreateKoStage()
	{
		return new Stage
		{
			Name = Res.KoStage,
			Rounds =
			[
				new()
				{
					Name = Res.RoundOf16,
					KoGames =
					[
						// Order is not chronological since UEFA is weird
						new(37, Qualifier.FromGroup("A1"), Qualifier.FromGroup("C2"),  new(2020, 6, 26, 21, 0, 0)),
						new(38, Qualifier.FromGroup("A2"), Qualifier.FromGroup("B2"),  new(2020, 6, 26, 18, 0, 0)),
						new(39, Qualifier.FromGroup("B1"), Qualifier.ThirdPlace("A/D/E/F"), new(2020, 6, 27, 21, 0, 0)),
						new(40, Qualifier.FromGroup("C1"), Qualifier.ThirdPlace("D/E/F"), new(2020, 6, 27, 18, 0, 0)),
						new(41, Qualifier.FromGroup("F1"), Qualifier.ThirdPlace("A/B/C"), new(2020, 6, 28, 21, 0, 0)),
						new(42, Qualifier.FromGroup("D2"), Qualifier.FromGroup("E2"), new(2020, 6, 28, 18, 0, 0)),
						new(43, Qualifier.FromGroup("E1"), Qualifier.ThirdPlace("A/B/C/D"), new(2020, 6, 29, 21, 0, 0)),
						new(44, Qualifier.FromGroup("D1"), Qualifier.FromGroup("F2"), new(2020, 6, 29, 18, 0, 0)),
					]
				},
				new()
				{
					Name = Res.Quarterfinal,
					KoGames =
					[
						new(45, Qualifier.FromGame(41), Qualifier.FromGame(42), new(2020, 7, 2, 18, 0, 0)),
						new(46, Qualifier.FromGame(39), Qualifier.FromGame(37), new(2020, 7, 2, 21, 0, 0)),
						new(47, Qualifier.FromGame(40), Qualifier.FromGame(38), new(2020, 7, 3, 18, 0, 0)),
						new(48, Qualifier.FromGame(43), Qualifier.FromGame(44), new(2020, 7, 3, 21, 0, 0)),
					]
				},
				new()
				{
					Name = Res.Semifinal,
					KoGames =
					[
						new(49, Qualifier.FromGame(46), Qualifier.FromGame(45), new(2020, 7, 6, 21, 0, 0)),
						new(50, Qualifier.FromGame(48), Qualifier.FromGame(47), new(2020, 7, 7, 21, 0, 0)),
					]
				},
				new()
				{
					Name = Res.Final,
					KoGames =
					[
						new(51, Qualifier.FromGame(49), Qualifier.FromGame(50), new(2020, 7, 11, 21, 0, 0)),
					],
				}
			]
		};
	}
}
