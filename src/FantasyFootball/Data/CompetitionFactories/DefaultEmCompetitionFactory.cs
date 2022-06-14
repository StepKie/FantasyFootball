namespace FantasyFootball.Data;

public class DefaultEmCompetitionFactory : DefaultTournamentFactory
{
	public DefaultEmCompetitionFactory(IRepository repo) : base(CompetitionType.EM, startDate: HistoricalData.EM_2020_START, noOfGroups: 6, groupSize: 4, repo) { }

	protected override List<Team> SelectParticipants() => new TeamSelector(_participantPool).DrawTeamsWeightedByElo(24, Confederation.UEFA);

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
							new() { HomeTeam = Groups[0].Teams[0], AwayTeam = Groups[0].Teams[2], PlayedOn = StartDate, },
							new() { HomeTeam = Groups[0].Teams[1], AwayTeam = Groups[0].Teams[3], PlayedOn = StartDate.AddHours(18) },
							new() { HomeTeam = Groups[1].Teams[0], AwayTeam = Groups[1].Teams[2], PlayedOn = StartDate.AddHours(21), },
							new() { HomeTeam = Groups[1].Teams[1], AwayTeam = Groups[1].Teams[3], PlayedOn = StartDate.AddHours(24), },
							new() { HomeTeam = Groups[2].Teams[0], AwayTeam = Groups[2].Teams[2], PlayedOn = StartDate.AddHours(42), },
							new() { HomeTeam = Groups[2].Teams[1], AwayTeam = Groups[2].Teams[3], PlayedOn = StartDate.AddHours(45), },
							new() { HomeTeam = Groups[3].Teams[0], AwayTeam = Groups[3].Teams[2], PlayedOn = StartDate.AddHours(48), },
							new() { HomeTeam = Groups[3].Teams[1], AwayTeam = Groups[3].Teams[3], PlayedOn = StartDate.AddHours(66), },
							new() { HomeTeam = Groups[4].Teams[0], AwayTeam = Groups[4].Teams[2], PlayedOn = StartDate.AddHours(69), },
							new() { HomeTeam = Groups[4].Teams[1], AwayTeam = Groups[4].Teams[3], PlayedOn = StartDate.AddHours(72), },
							new() { HomeTeam = Groups[5].Teams[0], AwayTeam = Groups[5].Teams[2], PlayedOn = StartDate.AddHours(93), },
							new() { HomeTeam = Groups[5].Teams[1], AwayTeam = Groups[5].Teams[3], PlayedOn = StartDate.AddHours(96), },
						}
					},
					new()
					{
						Name = Res.Round + " 2",
						Games = new()
						{
							new() { HomeTeam = Groups[0].Teams[0], AwayTeam = Groups[0].Teams[3], PlayedOn = StartDate.AddHours(114), },
							new() { HomeTeam = Groups[0].Teams[1], AwayTeam = Groups[0].Teams[2], PlayedOn = StartDate.AddHours(117), },
							new() { HomeTeam = Groups[1].Teams[0], AwayTeam = Groups[1].Teams[3], PlayedOn = StartDate.AddHours(120), },
							new() { HomeTeam = Groups[1].Teams[1], AwayTeam = Groups[1].Teams[2], PlayedOn = StartDate.AddHours(138), },
							new() { HomeTeam = Groups[2].Teams[0], AwayTeam = Groups[2].Teams[3], PlayedOn = StartDate.AddHours(141), },
							new() { HomeTeam = Groups[2].Teams[1], AwayTeam = Groups[2].Teams[2], PlayedOn = StartDate.AddHours(144), },
							new() { HomeTeam = Groups[3].Teams[0], AwayTeam = Groups[3].Teams[3], PlayedOn = StartDate.AddHours(162), },
							new() { HomeTeam = Groups[3].Teams[1], AwayTeam = Groups[3].Teams[2], PlayedOn = StartDate.AddHours(165), },
							new() { HomeTeam = Groups[4].Teams[0], AwayTeam = Groups[4].Teams[3], PlayedOn = StartDate.AddHours(168), },
							new() { HomeTeam = Groups[4].Teams[1], AwayTeam = Groups[4].Teams[2], PlayedOn = StartDate.AddHours(186), },
							new() { HomeTeam = Groups[5].Teams[0], AwayTeam = Groups[5].Teams[3], PlayedOn = StartDate.AddHours(189), },
							new() { HomeTeam = Groups[5].Teams[1], AwayTeam = Groups[5].Teams[2], PlayedOn = StartDate.AddHours(192), },
						}
					},
					new()
					{
						Name = Res.Round + " 3",
						Games = new()
						{
							new() { HomeTeam = Groups[0].Teams[0], AwayTeam = Groups[0].Teams[1], PlayedOn = StartDate.AddHours(213), },
							new() { HomeTeam = Groups[0].Teams[2], AwayTeam = Groups[0].Teams[3], PlayedOn = StartDate.AddHours(213), },
							new() { HomeTeam = Groups[1].Teams[0], AwayTeam = Groups[1].Teams[1], PlayedOn = StartDate.AddHours(237), },
							new() { HomeTeam = Groups[1].Teams[2], AwayTeam = Groups[1].Teams[3], PlayedOn = StartDate.AddHours(237), },
							new() { HomeTeam = Groups[2].Teams[0], AwayTeam = Groups[2].Teams[1], PlayedOn = StartDate.AddHours(240), },
							new() { HomeTeam = Groups[2].Teams[2], AwayTeam = Groups[2].Teams[3], PlayedOn = StartDate.AddHours(240), },
							new() { HomeTeam = Groups[3].Teams[0], AwayTeam = Groups[3].Teams[1], PlayedOn = StartDate.AddHours(264), },
							new() { HomeTeam = Groups[3].Teams[2], AwayTeam = Groups[3].Teams[3], PlayedOn = StartDate.AddHours(264), },
							new() { HomeTeam = Groups[4].Teams[0], AwayTeam = Groups[4].Teams[1], PlayedOn = StartDate.AddHours(285), },
							new() { HomeTeam = Groups[4].Teams[2], AwayTeam = Groups[4].Teams[3], PlayedOn = StartDate.AddHours(285), },
							new() { HomeTeam = Groups[5].Teams[0], AwayTeam = Groups[5].Teams[1], PlayedOn = StartDate.AddHours(288), },
							new() { HomeTeam = Groups[5].Teams[2], AwayTeam = Groups[5].Teams[3], PlayedOn = StartDate.AddHours(288), },
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
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe B", AwayTeamTentative = "3. Gruppe A/D/E/F", PlayedOn = StartDate.AddDays(16), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe A", AwayTeamTentative = "2. Gruppe C"      , PlayedOn = StartDate.AddDays(15), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe F", AwayTeamTentative = "3. Gruppe A/B/C"  , PlayedOn = StartDate.AddDays(17), },
							new() { IsKo = true, HomeTeamTentative = "2. Gruppe D", AwayTeamTentative = "2. Gruppe E"      , PlayedOn = StartDate.AddDays(17).AddHours(-3), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe E", AwayTeamTentative = "3. Gruppe A/B/C/D", PlayedOn = StartDate.AddDays(18), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe D", AwayTeamTentative = "2. Gruppe F"      , PlayedOn = StartDate.AddDays(18).AddHours(-3), },
							new() { IsKo = true, HomeTeamTentative = "1. Gruppe C", AwayTeamTentative = "3. Gruppe D/E/F"  , PlayedOn = StartDate.AddDays(16).AddHours(-3), },
							new() { IsKo = true, HomeTeamTentative = "2. Gruppe A", AwayTeamTentative = "2. Gruppe B"      , PlayedOn = StartDate.AddDays(15).AddHours(-3), },
						}
					},
					new()
					{
						Name = Res.Quarterfinal,
						Games = new()
						{
							new() { IsKo = true, HomeTeamTentative = "Viertelfinalist 1", AwayTeamTentative = "Viertelfinalist 2", PlayedOn = StartDate.AddDays(21).AddHours(-3), },
							new() { IsKo = true, HomeTeamTentative = "Viertelfinalist 3", AwayTeamTentative = "Viertelfinalist 4", PlayedOn = StartDate.AddDays(21), },
							new() { IsKo = true, HomeTeamTentative = "Viertelfinalist 5", AwayTeamTentative = "Viertelfinalist 6", PlayedOn = StartDate.AddDays(22).AddHours(-3), },
							new() { IsKo = true, HomeTeamTentative = "Viertelfinalist 7", AwayTeamTentative = "Viertelfinalist 8", PlayedOn = StartDate.AddDays(22) },
						}
					},
					new()
					{
						Name = Res.Semifinal,
						Games = new()
						{
							new() { IsKo = true, HomeTeamTentative = "Halbfinalist 1", AwayTeamTentative = "Halbfinalist 2", PlayedOn = StartDate.AddDays(25), },
							new() { IsKo = true, HomeTeamTentative = "Halbfinalist 3", AwayTeamTentative = "Halbfinalist 4", PlayedOn = StartDate.AddDays(26), },

						}
					},
					new()
					{
						Name = Res.Final,
						Games = new()
						{
							new() { IsKo = true, HomeTeamTentative = "Finalist 1", AwayTeamTentative = "Finalist 2", PlayedOn = StartDate.AddDays(30), },
						},
					}
				}
			}
		};
	}
}
