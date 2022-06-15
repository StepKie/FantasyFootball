namespace FantasyFootball.Data;

/// <summary> TODO In general, check how to best create Competitions for different scenarios. abstract strategy pattern seems not the most stable.
/// What about ITeamSelector, and what about GroupCreator/StageCreator.
/// What about creating historic competitions with known results.
/// </summary>
public static class CompetitionFactories
{
	public static readonly int[] HISTORIC_WM_YEARS = new[] { 1998, 2002, 2006, 2010, 2014, 2018, 2022 };

	public async static Task<Competition> Create(IRepository repo, CompetitionType selectedCompetitionType, TeamSelectionType selection)
	{
		CompetitionFactory factory = (selectedCompetitionType, selection) switch
		{
			(CompetitionType.EM, TeamSelectionType.HISTORIC) => new Em2020CompetitionFactory(repo),
			(CompetitionType.EM, TeamSelectionType.WITH_DRAWING) => new RandomEmCompetitionFactory(repo),
			(CompetitionType.WM, TeamSelectionType.HISTORIC) => new Wm2021CompetitionFactory(repo),
			_ => throw new ArgumentException($"No CompetitionFactory found for {selection}"),
		};

		return await factory.Create();
	}
}
