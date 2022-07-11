namespace FantasyFootball.Data.CompetitionFactories;

public abstract class CompetitionFactory
{
	public CompetitionType CompetitionType { get; init; }
	protected DateTime StartDate { get; init; }
	public virtual List<Group> Groups { get; protected set; }

	public CompetitionFactory(CompetitionType type, DateTime startDate, List<Group> groups)
	{
		CompetitionType = type;
		StartDate = startDate;
		Groups = groups;
	}

	public static CompetitionFactory For(CompetitionType type, int year, List<Group> groups = null)
	{
		CompetitionFactory factory = type switch
		{
			CompetitionType.EM => new EmCompetitionFactory(CompetitionType.EM.StartDate(year), groups),
			CompetitionType.WM => new WmCompetitionFactory(CompetitionType.WM.StartDate(year), groups),
			_ => throw new ArgumentException($"No CompetitionFactory found for {type}"),
		};

		return factory;
	}

	public static CompetitionFactory Default(CompetitionType type, IDataService dataService)
	{
		CompetitionFactory factory = type switch
		{
			CompetitionType.EM => EmCompetitionFactory.Default(dataService, HistoricalData.EM_2020_START.Year),
			CompetitionType.WM => WmCompetitionFactory.Default(dataService, HistoricalData.WM_2021_START.Year),
			_ => throw new ArgumentException($"No CompetitionFactory found for {type}"),
		};

		return factory;
	}

	public abstract List<Stage> CreateStages();

	[Time]
	/// <summary>
	/// Creates an (unsaved) competition
	/// </summary>
	/// <returns></returns>
	public virtual Competition Create()
	{
		if (Groups is null || !Groups.Any()) { throw new InvalidOperationException("Groups must be not empty or initialized before calling Create()"); }

		Competition competition = new()
		{
			Name = $"{CompetitionType.Name().Long} {StartDate.Year}",
			ShortName = $"{CompetitionType.Name().Short} {StartDate.Year}",
			Type = CompetitionType,
			SimulationStart = DateTime.Now,
			Stages = CreateStages(),
		};

		return competition;
	}
}
