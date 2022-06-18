namespace FantasyFootball.Data.CompetitionFactories;

public abstract class DefaultTournamentFactory : CompetitionFactory
{
	public int NoOfGroups { get; init; }
	public int GroupSize { get; init; }

	public DefaultTournamentFactory(CompetitionType type, DateTime startDate, int noOfGroups, int groupSize, IRepository repo) : base(type, startDate, repo)
	{
		NoOfGroups = noOfGroups;
		GroupSize = groupSize;
	}

	public override List<Group> CreateGroups()
	{
		var groups = "ABCDEFGHIJK".Take(NoOfGroups).Select(letter => new Group { Name = $"{Res.Group} {letter}", }).ToList();
		var teams = new Queue<Team>(Participants);
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

		return groups;
	}
}
