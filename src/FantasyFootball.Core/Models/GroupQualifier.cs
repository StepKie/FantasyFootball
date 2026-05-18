namespace FantasyFootball.Models;

[Table(nameof(GroupQualifier))]
public class GroupQualifier : Qualifier
{
	// used to cache the result, i.e. once the team is determined, store it here and return it in Get()
	Team? _qualified;

	public int GroupId { get; init; }
	public int FinalPlacement { get; init; }

	/// <summary> Expected format: group identifiers separated by slash, i.e. A/D/E/F </summary>
	public string ThirdPlaceCombination { get; init; }

	public Group? Group => Competition?.Groups[GroupId];

	// TODO Remove static reference to EuroRoundAdvancer
	public override Team? Get()
	{
		return _qualified ??= GetQualifier();

		Team? GetQualifier() => FinalPlacement switch
		{
			1 or 2 when Group?.IsFinished == true
				=> Group.GetStandings()[FinalPlacement - 1].Team,
			// Third-place resolution compares 3rd-place finishers across every group in
			// the stage (most-constrained-first slot assignment). All groups in the
			// stage must be finished — a per-group check would throw the moment any
			// single group finished, before the others.
			3 when Group?.Stage is { } stage && stage.Groups.All(g => g.IsFinished)
				=> TournamentFormatRegistry.ForGroupCount(stage.Groups.Count).ResolveThirdPlaceQualifier(stage, ThirdPlaceCombination),
			_ => null,
		};
	}

	public override Team GetPlaceholder() => new() { Name = $"{FinalPlacement}. {Group?.Name ?? GroupId.ToString()}", ShortName = "TBD", Type = TeamType.PLACEHOLDER, };
}
