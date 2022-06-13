namespace FantasyFootball.Data;

public abstract class DefaultTournamentFactory : CompetitionFactory
{
	public int NoOfGroups { get; init; }
	public int GroupSize { get; init; }

	public DefaultTournamentFactory(CompetitionType type, DateTime startDate, int noOfGroups, int groupSize, IRepository repo) : base(type, startDate, repo)
	{
		NoOfGroups = noOfGroups;
		GroupSize = groupSize;
	}

	protected override void CreateGroups()
	{
		SelectParticipants();
		Groups = "ABCDEFGHIJK".Take(NoOfGroups).Select(letter => new Group { Name = $"{Res.Group} {letter}", }).ToList();
		var teams = new Queue<Team>(Participants);
		while (teams.Any())
		{
			// Distribute teams into groups by selecting a random group from all groups with the least amount of teams in them
			var eligibleGroup = Groups.OrderBy(g => g.Teams.Count).ThenBy(g => new Guid()).First();
			eligibleGroup.Teams.Add(teams.Dequeue());
		}
	}
}
