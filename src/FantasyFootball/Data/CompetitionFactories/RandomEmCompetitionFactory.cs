namespace FantasyFootball.Data.CompetitionFactories;

public class RandomEmCompetitionFactory : DefaultTournamentFactory
{

	public RandomEmCompetitionFactory(IRepository repo) : base(CompetitionType.EM, startDate: HistoricalData.EM_2020_START, noOfGroups: 6, groupSize: 4, repo) { }

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
