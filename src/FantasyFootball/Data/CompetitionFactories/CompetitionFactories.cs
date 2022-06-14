namespace FantasyFootball.Data;

public static class CompetitionFactories
{
	public static int[] HISTORIC_WM_YEARS = new[] { 1998, 2002, 2006, 2010, 2014, 2018, 2022 };
	public async static Task<Competition> CreateEm(IRepository repo, TeamSelectionType selection)
	{
		return selection switch
		{
			TeamSelectionType.HISTORIC => await new Em2020CompetitionFactory(repo).Create(),
			TeamSelectionType.WITH_DRAWING => await new RandomEmCompetitionFactory(repo).Create(),
		};
	}

	public static Competition CreateEm(int year, ITeamSelector teamSelector)
	{
		return null;
	}
}
