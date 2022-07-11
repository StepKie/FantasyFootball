namespace FantasyFootball.Data.CompetitionFactories;

public class GroupFactory
{
	IDataService _dataService;

	public CompetitionType CompetitionType { get; init; }
	public int NoOfGroups { get; init; }
	public int GroupSize { get; init; }

	public GroupFactory(CompetitionType type, int noOfGroups, int groupSize, IDataService dataService)
	{
		_dataService = dataService;
		CompetitionType = type;
		NoOfGroups = noOfGroups;
		GroupSize = groupSize;
	}

	public static GroupFactory For(IDataService dataService, CompetitionType type) => type switch
	{
		CompetitionType.EM => new(CompetitionType.EM, noOfGroups: 6, groupSize: 4, dataService),
		CompetitionType.WM => new(CompetitionType.WM, noOfGroups: 8, groupSize: 4, dataService),
		_ => throw new InvalidOperationException(),
	};

	public List<Group> DrawRandom()
	{
		Confederation? confederation = CompetitionType == CompetitionType.EM ? Confederation.UEFA : null;
		var participants = DrawTeamsWeightedByElo(NoOfGroups * GroupSize, confederation);
		var groups = "ABCDEFGHIJK".Take(NoOfGroups).Select(letter => new Group { Name = $"{Res.Group} {letter}" }).ToList();
		var teams = new Queue<Team>(participants);
		while (teams.Any())
		{
			// Distribute teams into groups by selecting a random group from all groups with the least amount of teams in them
			var eligibleGroup = groups.Where(g => g.Teams.Count < GroupSize).OrderBy(g => g.Teams.Count).Shuffle().FirstOrDefault();
			if (eligibleGroup is null)
			{
				break;
			}

			eligibleGroup.Teams.Add(teams.Dequeue());
		}

		List<Team> DrawTeamsWeightedByElo(int amount, Confederation? from = null)
		{
			var eligibleTeams = _dataService.AllTeams.Where(t => from is null || t.Country.Confederation.Equals(from));
			// Each team gets (team.elo - 1000 (but a minimum of 10)) balls into the drawing urn ...
			var urn = eligibleTeams.SelectMany(t => Enumerable.Repeat(t, Math.Max(t.Elo - 1000, 10))).ToList();
			// Order balls randomly, then remove duplicates and draw the desired amount
			var selected = urn.Distinct().Shuffle().Take(amount).OrderByDescending(t => t.Elo).ToList();

			return selected;
		}

		return groups;
	}

	public List<Group> CreateFromHistoricalData(int year)
	{
		Dictionary<string, string[]> historicalData = (year, CompetitionType) switch
		{
			(2020, CompetitionType.EM) => HistoricalData.EM_2020_TEAMS,
			(2016, CompetitionType.EM) => HistoricalData.EM_2016_TEAMS,
			(2021, CompetitionType.WM) => HistoricalData.WM_2021_TEAMS,
			(2018, CompetitionType.WM) => HistoricalData.WM_2018_TEAMS,
			_ => throw new ArgumentException($"No historical data for {CompetitionType}"),
		};

		List<Group> groups = new();

		foreach (var entry in historicalData)
		{
			Group group = new() { Name = $"{Res.Group} {entry.Key}" };
			List<Team> teamsInGroup = entry.Value.Select(shortName => Team(shortName)).ToList();
			group.Teams.AddRange(teamsInGroup);
			groups.Add(group);
		}

		return groups;

		Team Team(string shortName) => _dataService.AllTeams.FirstOrDefault(t => t.ShortName == shortName) ?? throw new ArgumentException($"Team {shortName} not found in db");
	}
}
