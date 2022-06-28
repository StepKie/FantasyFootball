namespace FantasyFootball.ViewModels;

public class TeamsGroup : List<TeamViewModel>
{
	public Group Group { get; init; }
	public string Name { get; private set; }

	public TeamsGroup(Group group)
	{
		Group = group;
		Name = Group.Name;

		IEnumerable<TeamViewModel> tvms = Group.Teams.Select((t, index) => new TeamViewModel(index + 1, t));
		AddRange(tvms);

	}
}
