namespace FantasyFootball.Models;

[Table(nameof(GroupQualifer))]
public class GroupQualifer : Qualifier
{
	public int GroupId { get; init; }
	public int FinalPlacement { get; init; }

	public override Team Get()
	{
		var group = Competition.Groups[GroupId];
		if (!Competition.Stages[0].IsFinished)
		{
			return new Team() { Name = $"{FinalPlacement}. {group.Name}" };
		}

		return Competition.Groups[GroupId].GetStandings()[FinalPlacement].Team;
	}
}
