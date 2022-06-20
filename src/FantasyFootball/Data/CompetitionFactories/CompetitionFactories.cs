namespace FantasyFootball.Data.CompetitionFactories;

/// <summary> TODO In general, check how to best create Competitions for different scenarios. abstract strategy pattern seems not the most stable.
/// What about ITeamSelector, and what about GroupCreator/StageCreator.
/// What about creating historic competitions with known results.
/// </summary>
public static class CompetitionFactories
{
	public static readonly int[] HISTORIC_WM_YEARS = new[] { 2018, 2022 };
	public static readonly int[] HISTORIC_EM_YEARS = new[] { 2016, 2020 };

	public static async Task<Competition> Create(IRepository repo, CompetitionType selectedCompetitionType, TeamSelectionType selection)
	{
		CompetitionFactory factory = (selectedCompetitionType, selection) switch
		{
			(CompetitionType.EM, TeamSelectionType.HISTORIC) => new Em2020CompetitionFactory(repo),
			(CompetitionType.EM, TeamSelectionType.WITH_DRAWING) => new DefaultEmCompetitionFactory(repo),
			(CompetitionType.WM, TeamSelectionType.HISTORIC) => new Wm2022CompetitionFactory(repo),
			_ => throw new ArgumentException($"No CompetitionFactory found for {selection}"),
		};

		return await factory.Create();
	}

	public static IEnumerable<int> AvailableYears(this CompetitionType type)
	{
		return type switch
		{
			CompetitionType.EM => HISTORIC_EM_YEARS,
			CompetitionType.WM => HISTORIC_WM_YEARS,
			_ => Enumerable.Empty<int>(),
		};
	}
}
