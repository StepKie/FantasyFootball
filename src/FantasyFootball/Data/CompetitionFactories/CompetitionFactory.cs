namespace FantasyFootball.Data.CompetitionFactories;

public abstract class CompetitionFactory
{
	public CompetitionType CompetitionType { get; init; }
	protected DateTime StartDate { get; init; }
	public virtual List<Group> Groups { get; protected set; }

	protected CompetitionFactory(CompetitionType type, DateTime startDate, List<Group> groups)
	{
		CompetitionType = type;
		StartDate = startDate;
		Groups = groups;
	}

	public static CompetitionFactory For(CompetitionType type, int year, List<Group> groups)
	{
		CompetitionFactory factory = type switch
		{
			CompetitionType.EM => new EmCompetitionFactory(CompetitionType.EM.StartDate(year), groups),
			CompetitionType.WM => new WmCompetitionFactory(CompetitionType.WM.StartDate(year), groups),
			CompetitionType.CHAMPIONS_LEAGUE => throw new NotImplementedException(),
			CompetitionType.DOMESTIC_LEAGUE => throw new NotImplementedException(),
			_ => throw new ArgumentException($"No CompetitionFactory yet implemented for {type}"),
		};

		return factory;
	}

	public static CompetitionFactory Default(CompetitionType type, IDataService dataService, int year)
	{
		CompetitionFactory factory = type switch
		{
			CompetitionType.EM => EmCompetitionFactory.Default(dataService, year),
			CompetitionType.WM => WmCompetitionFactory.Default(dataService, year),
			CompetitionType.CHAMPIONS_LEAGUE => throw new NotImplementedException(),
			CompetitionType.DOMESTIC_LEAGUE => throw new NotImplementedException(),
			_ => throw new ArgumentException($"No CompetitionFactory found for {type}"),
		};

		return factory;
	}

	public abstract List<Stage> CreateStages();

	/// <summary>
	/// Creates an (unsaved) competition
	/// </summary>
	/// <returns></returns>
	public virtual Competition Create()
	{
		if (Groups is null || Groups.Count == 0) { throw new InvalidOperationException("Groups must be not empty or initialized before calling Create()"); }

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

	/// <summary> Matchup string is in the format "B1 - C3", i.e. group identifiers plus place identifiers starting at 1 </summary>
	protected Game Create(string matchup, int dayOffset, int hourOffset)
	{
		var letterPlusPlace = matchup.Split("-", options: StringSplitOptions.TrimEntries);
		var home = letterPlusPlace[0];
		var away = letterPlusPlace[1];
		var homeGroup = "ABCDEFGH".IndexOf(home[0]);
		var awayGroup = "ABCDEFGH".IndexOf(away[0]);

		var homePlace = int.Parse(home[1..]) - 1;
		var awayPlace = int.Parse(away[1..]) - 1;

		return new Game
		{
			HomeTeam = Groups[homeGroup].Teams[homePlace],
			AwayTeam = Groups[awayGroup].Teams[awayPlace],
			PlayedOn = StartDate + TimeSpan.FromDays(dayOffset) + TimeSpan.FromHours(hourOffset),
		};
	}
}
