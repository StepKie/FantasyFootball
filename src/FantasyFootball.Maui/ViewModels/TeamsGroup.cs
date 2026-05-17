namespace FantasyFootball.ViewModels;

public class TeamsGroup : List<TeamViewModel>
{
	public Group Group { get; init; }
	public string Name { get; private set; }

	public TeamsGroup(Group group)
	{
		Group = group;
		Name = Group.Name;

		IEnumerable<TeamViewModel> tvms = Group.Teams.Select((t, index) => TeamViewModel.Create(index + 1, t.Id));
		AddRange(tvms);

	}
}
