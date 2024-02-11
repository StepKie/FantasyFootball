namespace FantasyFootball.Data.CompetitionFactories;

public class GroupFactory(CompetitionType type, int noOfGroups, int groupSize, IDataService dataService)
{

	public CompetitionType CompetitionType { get; init; } = type;
	public int NoOfGroups { get; init; } = noOfGroups;
	public int GroupSize { get; init; } = groupSize;

	public static GroupFactory For(IDataService dataService, CompetitionType type) => type switch
	{
		CompetitionType.EM => new(CompetitionType.EM, noOfGroups: 6, groupSize: 4, dataService),
		CompetitionType.WM => new(CompetitionType.WM, noOfGroups: 8, groupSize: 4, dataService),
		CompetitionType.CHAMPIONS_LEAGUE => throw new NotImplementedException(),
		CompetitionType.DOMESTIC_LEAGUE => throw new NotImplementedException(),
		_ => throw new InvalidOperationException(),
	};

	public List<Group> DrawRandom()
	{
		var confederation = CompetitionType == CompetitionType.EM ? Confederation.UEFA : null;
		var participants = DrawTeamsWeightedByElo(NoOfGroups * GroupSize, confederation);
		var groups = "ABCDEFGHIJK".Take(NoOfGroups).Select(letter => new Group { Name = $"{Res.Group} {letter}" }).ToList();
		var teams = new Queue<Team>(participants);
		while (teams.Count != 0)
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
			var eligibleTeams = dataService.AllTeams.Where(t => from is null || t.Country.Confederation.Equals(from));
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
			(2024, CompetitionType.EM) => HistoricalData.EM_2024_TEAMS,
			(2020, CompetitionType.EM) => HistoricalData.EM_2020_TEAMS,
			(2016, CompetitionType.EM) => HistoricalData.EM_2016_TEAMS,
			(2022, CompetitionType.WM) => HistoricalData.WM_2022_TEAMS,
			(2018, CompetitionType.WM) => HistoricalData.WM_2018_TEAMS,
			_ => throw new ArgumentException($"No historical data for {CompetitionType}"),
		};

		List<Group> groups = [];

		foreach (var entry in historicalData)
		{
			Group group = new() { Name = $"{Res.Group} {entry.Key}" };
			List<Team> teamsInGroup = entry.Value.Select(Team).ToList();
			group.Teams.AddRange(teamsInGroup);
			groups.Add(group);
		}

		return groups;

		Team Team(string shortName) => dataService.AllTeams.FirstOrDefault(t => t.ShortName == shortName) ?? throw new ArgumentException($"Team {shortName} not found in db");
	}
}
