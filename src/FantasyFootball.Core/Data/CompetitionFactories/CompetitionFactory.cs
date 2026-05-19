namespace FantasyFootball.Data.CompetitionFactories;

public abstract class CompetitionFactory
{
	public CompetitionType CompetitionType { get; init; }
	protected DateTime StartDate { get; init; }
	public virtual List<Group> Groups { get; protected set; }

	/// <summary>
	/// The structural format of this tournament — group layout and third-place advancement rules.
	/// Multiple editions can share a format (e.g., Euro 2024 and Euro 2028 both use <see cref="EuroFormat"/>).
	/// </summary>
	public abstract ITournamentFormat Format { get; }

	protected CompetitionFactory(CompetitionType type, DateTime startDate, List<Group> groups)
	{
		CompetitionType = type;
		StartDate = startDate;
		Groups = groups;
	}

	public static CompetitionFactory For(CompetitionType type, int year, List<Group> groups)
	{
		CompetitionFactory factory = (type, year) switch
		{
			(CompetitionType.EM, _) => new EmCompetitionFactory(CompetitionType.EM.StartDate(year), groups),
			(CompetitionType.WM, 2026) => new Wm2026CompetitionFactory(HistoricalData.WM_2026_START, groups),
			(CompetitionType.WM, _) => new WmCompetitionFactory(CompetitionType.WM.StartDate(year), groups),
			(CompetitionType.CHAMPIONS_LEAGUE, _) => throw new NotImplementedException(),
			(CompetitionType.DOMESTIC_LEAGUE, _) => throw new NotImplementedException(),
			_ => throw new ArgumentException($"No CompetitionFactory yet implemented for {type}"),
		};

		return factory;
	}

	public static CompetitionFactory Default(CompetitionType type, IDataService dataService, int year)
	{
		CompetitionFactory factory = (type, year) switch
		{
			(CompetitionType.EM, _) => EmCompetitionFactory.Default(dataService, year),
			(CompetitionType.WM, 2026) => Wm2026CompetitionFactory.Default(dataService),
			(CompetitionType.WM, _) => WmCompetitionFactory.Default(dataService, year),
			(CompetitionType.CHAMPIONS_LEAGUE, _) => throw new NotImplementedException(),
			(CompetitionType.DOMESTIC_LEAGUE, _) => throw new NotImplementedException(),
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

		// Defensive clone: callers (CompetitionSetupViewModel) often hold a single
		// Groups list across multiple Create() calls — ResetToHistoricTeams only
		// refreshes it on year change. Two Create()s with the same input list
		// produce two Competitions whose Stage.Groups point to THE SAME Group
		// instances. WireBackReferences for the second Competition then overwrites
		// the first's group.Stage back-pointer, and bucket-serialize writes
		// invalid Preserve JSON with forward $refs (MetadataReferenceNotFound on
		// next load). Cloning before CreateStages binds the cloned list to this
		// Competition's Stages + Games for the rest of the build.
		Groups = Groups.Select(g => new Group { Name = g.Name, Teams = [.. g.Teams] }).ToList();

		Competition competition = new()
		{
			Name = $"{CompetitionType.Name().Long} {StartDate.Year}",
			ShortName = $"{CompetitionType.Name().Short} {StartDate.Year}",
			Type = CompetitionType,
			SimulationStart = DateTime.Now,
			Stages = CreateStages(),
		};

		WireBackReferences(competition);
		return competition;
	}

	/// <summary>
	/// Sets the back-references the factories' object initializers don't populate
	/// (<c>Stage.Competition</c>, <c>Group.Stage</c>, <c>Round.Stage</c>, <c>Game.Round</c>).
	/// MAUI/SQLite gets these hydrated from foreign keys on read via sqlite-net-extensions;
	/// the Web path keeps the in-memory graph after Create and would NRE on
	/// <c>Group.Games</c> / <c>Game.Round.Stage</c> without explicit wiring.
	/// </summary>
	static void WireBackReferences(Competition competition)
	{
		foreach (var stage in competition.Stages)
		{
			stage.Competition = competition;
			foreach (var group in stage.Groups) { group.Stage = stage; }
			foreach (var round in stage.Rounds)
			{
				round.Stage = stage;
				foreach (var game in round.AllGames)
				{
					game.Round = round;
					// KoGame qualifiers reach Competition via qualifier.Game → Round → Stage → Competition.
					// Without this, GroupQualifier.Group resolves null and KoGame.HomeTeam stays a placeholder
					// (so the game never becomes IsReadyToStart and the sim button silently no-ops).
					if (game is KoGame ko)
					{
						if (ko.HomeGroupQualifier is not null) { ko.HomeGroupQualifier.Game = ko; }
						if (ko.AwayGroupQualifier is not null) { ko.AwayGroupQualifier.Game = ko; }
						if (ko.HomeGameQualifier is not null) { ko.HomeGameQualifier.Game = ko; }
						if (ko.AwayGameQualifier is not null) { ko.AwayGameQualifier.Game = ko; }
					}
				}
			}
		}
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
