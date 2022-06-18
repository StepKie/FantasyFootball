namespace FantasyFootball.Models;

[Table(nameof(GroupQualifer))]
public class GroupQualifer : Qualifier
{
	public int GroupId { get; init; }
	public int FinalPlacement { get; init; }

	public Group Group => Competition.Groups[GroupId];

	public override Team? Get() => Competition.Stages[0].IsFinished ? Group.GetStandings()[FinalPlacement - 1].Team : null;

	public override Team GetStandin() => new() { Name = $"{FinalPlacement}. {Group.Name}", ShortName = "TBD" };
}
