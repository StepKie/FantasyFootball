namespace FantasyFootball.Models;

/// <summary> TODO Check, this has no effect when inspecting the GroupId of given Teams in a group </summary>
[Table(nameof(TeamGroupAssignment))]
public class TeamGroupAssignment
{
	[ForeignKey(typeof(Team))] public int TeamId { get; set; }
	[ForeignKey(typeof(Group))] public int GroupId { get; set; }
}
