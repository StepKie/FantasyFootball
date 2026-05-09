namespace FantasyFootball.Data.CompetitionFactories;

/// <summary>
/// FIFA World Cup 2026 — 48 teams, 12 groups (A–L) of 4, with an additional Round of 32.
/// All match dates/times are in US Eastern Time (EDT, UTC-4) for consistency across the
/// USA/Canada/Mexico-hosted tournament.
/// </summary>
public class Wm2026CompetitionFactory(DateTime start, List<Group> groups) : CompetitionFactory(CompetitionType.WM, start, groups)
{
	public override ITournamentFormat Format => ExpandedWorldCupFormat.Instance;

	public static Wm2026CompetitionFactory Default(IDataService dataService) =>
		new(HistoricalData.WM_2026_START, GroupFactory.For(dataService, CompetitionType.WM, 2026).CreateFromHistoricalData(2026));

	public override List<Stage> CreateStages() => [CreateGroupStage(), CreateKoStage()];

	Stage CreateGroupStage()
	{
		Game G(int groupIdx, int home, int away, int year, int month, int day, int hour, int minute = 0)
			=> new() { HomeTeam = Groups[groupIdx].Teams[home], AwayTeam = Groups[groupIdx].Teams[away], PlayedOn = new(year, month, day, hour, minute, 0) };

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
						G(0, 0, 1, 2026, 6, 11, 15),  // MEX vs RSA — Mexico City
						G(0, 2, 3, 2026, 6, 11, 22),  // KOR vs CZE — Zapopan
						G(1, 0, 1, 2026, 6, 12, 15),  // CAN vs BIH — Toronto
						G(3, 0, 1, 2026, 6, 12, 21),  // USA vs PAR — LA
						G(1, 2, 3, 2026, 6, 13, 15),  // QAT vs SUI — Santa Clara
						G(2, 0, 1, 2026, 6, 13, 18),  // BRA vs MAR — NJ
						G(2, 2, 3, 2026, 6, 13, 21),  // HAI vs SCO — Foxborough
						G(3, 2, 3, 2026, 6, 14,  0),  // AUS vs TUR — Vancouver
						G(4, 0, 1, 2026, 6, 14, 13),  // GER vs CUW — Houston
						G(5, 0, 1, 2026, 6, 14, 16),  // NED vs JPN — Arlington
						G(4, 2, 3, 2026, 6, 14, 19),  // CIV vs ECU — Philadelphia
						G(5, 2, 3, 2026, 6, 14, 22),  // SWE vs TUN — Guadalupe
						G(7, 0, 1, 2026, 6, 15, 12),  // ESP vs CPV — Atlanta
						G(6, 0, 1, 2026, 6, 15, 15),  // BEL vs EGY — Seattle
						G(7, 2, 3, 2026, 6, 15, 18),  // KSA vs URU — Miami
						G(6, 2, 3, 2026, 6, 15, 21),  // IRN vs NZL — LA
						G(8, 0, 1, 2026, 6, 16, 15),  // FRA vs SEN — NJ
						G(8, 2, 3, 2026, 6, 16, 18),  // IRQ vs NOR — Foxborough
						G(9, 0, 1, 2026, 6, 16, 21),  // ARG vs ALG — Kansas City
						G(9, 2, 3, 2026, 6, 17,  0),  // AUT vs JOR — Santa Clara
						G(10, 0, 1, 2026, 6, 17, 13), // POR vs COD — Houston
						G(11, 0, 1, 2026, 6, 17, 16), // ENG vs CRO — Arlington
						G(11, 2, 3, 2026, 6, 17, 19), // GHA vs PAN — Toronto
						G(10, 2, 3, 2026, 6, 17, 22), // UZB vs COL — Mexico City
					]
				},
				new()
				{
					Name = Res.Round + " 2",
					RegularGames =
					[
						G(0, 3, 1, 2026, 6, 18, 12),  // CZE vs RSA — Atlanta
						G(1, 3, 1, 2026, 6, 18, 15),  // SUI vs BIH — LA
						G(1, 0, 2, 2026, 6, 18, 18),  // CAN vs QAT — Vancouver
						G(0, 0, 2, 2026, 6, 18, 21),  // MEX vs KOR — Zapopan
						G(3, 0, 2, 2026, 6, 19, 15),  // USA vs AUS — Seattle
						G(2, 3, 1, 2026, 6, 19, 18),  // SCO vs MAR — Foxborough
						G(2, 0, 2, 2026, 6, 19, 20, 30), // BRA vs HAI — Philadelphia
						G(3, 3, 1, 2026, 6, 19, 23),  // TUR vs PAR — Santa Clara
						G(5, 0, 2, 2026, 6, 20, 13), // NED vs SWE — Houston
						G(4, 0, 2, 2026, 6, 20, 16), // GER vs CIV — Toronto
						G(4, 3, 1, 2026, 6, 20, 20), // ECU vs CUW — Kansas City
						G(5, 3, 1, 2026, 6, 21,  0), // TUN vs JPN — Guadalupe
						G(7, 0, 2, 2026, 6, 21, 12), // ESP vs KSA — Atlanta
						G(6, 0, 2, 2026, 6, 21, 15), // BEL vs IRN — LA
						G(7, 3, 1, 2026, 6, 21, 18), // URU vs CPV — Miami
						G(6, 3, 1, 2026, 6, 21, 21), // NZL vs EGY — Vancouver
						G(9, 0, 2, 2026, 6, 22, 13), // ARG vs AUT — Arlington
						G(8, 0, 2, 2026, 6, 22, 17), // FRA vs IRQ — Philadelphia
						G(8, 3, 1, 2026, 6, 22, 20), // NOR vs SEN — Toronto
						G(9, 3, 1, 2026, 6, 22, 23), // JOR vs ALG — Santa Clara
						G(10, 0, 2, 2026, 6, 23, 13), // POR vs UZB — Houston
						G(11, 0, 2, 2026, 6, 23, 16), // ENG vs GHA — Foxborough
						G(11, 3, 1, 2026, 6, 23, 19), // PAN vs CRO — Foxborough
						G(10, 3, 1, 2026, 6, 23, 22), // COL vs COD — Zapopan
					]
				},
				new()
				{
					Name = Res.Round + " 3",
					RegularGames =
					[
						G(1, 3, 0, 2026, 6, 24, 15), // SUI vs CAN — Vancouver
						G(1, 1, 2, 2026, 6, 24, 15), // BIH vs QAT — Seattle
						G(2, 1, 2, 2026, 6, 24, 18), // MAR vs HAI — Atlanta
						G(2, 3, 0, 2026, 6, 24, 18), // SCO vs BRA — Miami
						G(0, 1, 2, 2026, 6, 24, 21), // RSA vs KOR — Guadalupe
						G(0, 3, 0, 2026, 6, 24, 21), // CZE vs MEX — Mexico City
						G(4, 1, 2, 2026, 6, 25, 16), // CUW vs CIV — Philadelphia
						G(4, 3, 0, 2026, 6, 25, 16), // ECU vs GER — NJ
						G(5, 3, 0, 2026, 6, 25, 19), // TUN vs NED — Kansas City
						G(5, 1, 2, 2026, 6, 25, 19), // JPN vs SWE — Arlington
						G(3, 3, 0, 2026, 6, 25, 22), // TUR vs USA — LA
						G(3, 1, 2, 2026, 6, 25, 22), // PAR vs AUS — Santa Clara
						G(8, 3, 0, 2026, 6, 26, 15), // NOR vs FRA — Foxborough
						G(8, 1, 2, 2026, 6, 26, 15), // SEN vs IRQ — Toronto
						G(7, 1, 2, 2026, 6, 26, 20), // CPV vs KSA — Houston
						G(7, 3, 0, 2026, 6, 26, 20), // URU vs ESP — Zapopan
						G(6, 3, 0, 2026, 6, 26, 23), // NZL vs BEL — Vancouver
						G(6, 1, 2, 2026, 6, 26, 23), // EGY vs IRN — Seattle
						G(11, 3, 0, 2026, 6, 27, 17), // PAN vs ENG — NJ
						G(11, 1, 2, 2026, 6, 27, 17), // CRO vs GHA — Philadelphia
						G(10, 3, 0, 2026, 6, 27, 19, 30), // COL vs POR — Miami
						G(10, 1, 2, 2026, 6, 27, 19, 30), // COD vs UZB — Atlanta
						G(9, 1, 2, 2026, 6, 27, 22), // ALG vs AUT — Kansas City
						G(9, 3, 0, 2026, 6, 27, 22), // JOR vs ARG — Arlington
					]
				}
			]
		};
	}

	Stage CreateKoStage()
	{
		return new Stage
		{
			Name = Res.KoStage,
			Rounds =
			[
				new()
				{
					Name = Res.RoundOf32,
					KoGames =
					[
						new(73, Qualifier.FromGroup("A2"), Qualifier.FromGroup("B2"), new(2026, 6, 28, 15, 0, 0)),
						new(74, Qualifier.FromGroup("E1"), Qualifier.ThirdPlace("A/B/C/D/F"), new(2026, 6, 29, 16, 30, 0)),
						new(75, Qualifier.FromGroup("F1"), Qualifier.FromGroup("C2"), new(2026, 6, 29, 21, 0, 0)),
						new(76, Qualifier.FromGroup("C1"), Qualifier.FromGroup("F2"), new(2026, 6, 29, 13, 0, 0)),
						new(77, Qualifier.FromGroup("I1"), Qualifier.ThirdPlace("C/D/F/G/H"), new(2026, 6, 30, 17, 0, 0)),
						new(78, Qualifier.FromGroup("E2"), Qualifier.FromGroup("I2"), new(2026, 6, 30, 13, 0, 0)),
						new(79, Qualifier.FromGroup("A1"), Qualifier.ThirdPlace("C/E/F/H/I"), new(2026, 6, 30, 21, 0, 0)),
						new(80, Qualifier.FromGroup("L1"), Qualifier.ThirdPlace("E/H/I/J/K"), new(2026, 7,  1, 12, 0, 0)),
						new(81, Qualifier.FromGroup("D1"), Qualifier.ThirdPlace("B/E/F/I/J"), new(2026, 7,  1, 20, 0, 0)),
						new(82, Qualifier.FromGroup("G1"), Qualifier.ThirdPlace("A/E/H/I/J"), new(2026, 7,  1, 16, 0, 0)),
						new(83, Qualifier.FromGroup("K2"), Qualifier.FromGroup("L2"), new(2026, 7,  2, 19, 0, 0)),
						new(84, Qualifier.FromGroup("H1"), Qualifier.FromGroup("J2"), new(2026, 7,  2, 15, 0, 0)),
						new(85, Qualifier.FromGroup("B1"), Qualifier.ThirdPlace("E/F/G/I/J"), new(2026, 7,  2, 23, 0, 0)),
						new(86, Qualifier.FromGroup("J1"), Qualifier.FromGroup("H2"), new(2026, 7,  3, 18, 0, 0)),
						new(87, Qualifier.FromGroup("K1"), Qualifier.ThirdPlace("D/E/I/J/L"), new(2026, 7,  3, 21, 30, 0)),
						new(88, Qualifier.FromGroup("D2"), Qualifier.FromGroup("G2"), new(2026, 7,  3, 14, 0, 0)),
					]
				},
				new()
				{
					Name = Res.RoundOf16,
					KoGames =
					[
						new(89, Qualifier.FromGame(74), Qualifier.FromGame(77), new(2026, 7, 4, 17, 0, 0)),
						new(90, Qualifier.FromGame(73), Qualifier.FromGame(75), new(2026, 7, 4, 13, 0, 0)),
						new(91, Qualifier.FromGame(76), Qualifier.FromGame(78), new(2026, 7, 5, 16, 0, 0)),
						new(92, Qualifier.FromGame(79), Qualifier.FromGame(80), new(2026, 7, 5, 20, 0, 0)),
						new(93, Qualifier.FromGame(83), Qualifier.FromGame(84), new(2026, 7, 6, 15, 0, 0)),
						new(94, Qualifier.FromGame(81), Qualifier.FromGame(82), new(2026, 7, 6, 20, 0, 0)),
						new(95, Qualifier.FromGame(86), Qualifier.FromGame(88), new(2026, 7, 7, 12, 0, 0)),
						new(96, Qualifier.FromGame(85), Qualifier.FromGame(87), new(2026, 7, 7, 16, 0, 0)),
					]
				},
				new()
				{
					Name = Res.Quarterfinal,
					KoGames =
					[
						new(97, Qualifier.FromGame(89), Qualifier.FromGame(90), new(2026, 7,  9, 16, 0, 0)),
						new(98, Qualifier.FromGame(93), Qualifier.FromGame(94), new(2026, 7, 10, 15, 0, 0)),
						new(99, Qualifier.FromGame(91), Qualifier.FromGame(92), new(2026, 7, 11, 17, 0, 0)),
						new(100, Qualifier.FromGame(95), Qualifier.FromGame(96), new(2026, 7, 11, 21, 0, 0)),
					]
				},
				new()
				{
					Name = Res.Semifinal,
					KoGames =
					[
						new(101, Qualifier.FromGame(97), Qualifier.FromGame(98), new(2026, 7, 14, 15, 0, 0)),
						new(102, Qualifier.FromGame(99), Qualifier.FromGame(100), new(2026, 7, 15, 15, 0, 0)),
					]
				},
				new()
				{
					Name = Res.ThirdPlaceMatch,
					KoGames =
					[
						new(103, Qualifier.FromGame(101, loserQualifies: true), Qualifier.FromGame(102, loserQualifies: true), new(2026, 7, 18, 17, 0, 0)),
					],
				},
				new()
				{
					Name = Res.Final,
					KoGames =
					[
						new(104, Qualifier.FromGame(101), Qualifier.FromGame(102), new(2026, 7, 19, 15, 0, 0)),
					],
				}
			]
		};
	}
}
