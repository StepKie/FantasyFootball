namespace FantasyFootball.Models;

[Table(nameof(GroupQualifier))]
public class GroupQualifier : Qualifier
{
	// used to cache the result, i.e. once the team is determined, store it here and return it in Get()
	private Team _qualified;

	public int GroupId { get; init; }
	public int FinalPlacement { get; init; }

	/// <summary> Expected format: group identifiers separated by slash, i.e. A/D/E/F </summary>
	public string ThirdPlaceCombination { get; init; }

	public Group? Group => Competition?.Groups[GroupId];

	// TODO Remove static reference to EuroRoundAdvancer
	public override Team? Get()
	{
		return (FinalPlacement, Group?.IsFinished) switch
		{
			(1 or 2, true) => _qualified ??= Group.GetStandings()[FinalPlacement - 1].Team,
			(3, true) => _qualified ??= EuroRoundAdvancer.GetThirdPlaceQualifier(Group.Stage, ThirdPlaceCombination),
			_ => null,
		};
	}

	public override Team GetPlaceholder() => new() { Name = $"{FinalPlacement}. {Group?.Name ?? GroupId.ToString()}", ShortName = "TBD", Type = TeamType.PLACEHOLDER, };
}
