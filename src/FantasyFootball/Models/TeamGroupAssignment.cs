namespace FantasyFootball.Models;

public class TeamGroupAssignment
{
	[ForeignKey(typeof(Team))] public int TeamId { get; set; }
	[ForeignKey(typeof(Group))] public int GroupId { get; set; }
}
