namespace FantasyFootball.Models;

[Table(nameof(GroupQualifier))]
public class GroupQualifier : Qualifier
{
	public int GroupId { get; init; }
	public int FinalPlacement { get; init; }

	/// <summary> Expected format: group identifiers separated by slash, i.e. A/D/E/F </summary>
	public string ThirdPlaceCombination { get; init; }

	public Group? Group => Competition?.Groups[GroupId];

	// Recomputes each call. Caching the resolved team led to stale KO bracket display after an
	// undo on a group-stage game, because the cache had no invalidation hook. The recompute is
	// cheap (one GetStandings pass over a 4-team group, or a single ResolveThirdPlaceQualifier
	// call once the stage finishes) and the IsFinished guards inside ensure null is returned
	// whenever the source state isn't ready.
	public override Team? Get()
	{
		return GetQualifier();

		Team? GetQualifier() => FinalPlacement switch
		{
			1 or 2 when Group?.IsFinished == true
				=> Group.GetStandings()[FinalPlacement - 1].Team,
			// Third-place resolution compares 3rd-place finishers across every group in
			// the stage (most-constrained-first slot assignment). All groups in the
			// stage must be finished — a per-group check would throw the moment any
			// single group finished, before the others.
			3 when Group?.Stage is { } stage && stage.Groups.All(g => g.IsFinished)
				=> TryResolveThirdPlace(stage, ThirdPlaceCombination),
			_ => null,
		};

		// The 3rd-place allocation in ExpandedWorldCupFormat can occasionally fail
		// to fit every slot — the real fix is FIFA's 495-scenario lookup table.
		// Falling back to null lets the UI show a placeholder instead of crashing
		// the render or breaking JSON persistence.
		static Team? TryResolveThirdPlace(Stage stage, string combination)
		{
			try { return TournamentFormatRegistry.ForGroupCount(stage.Groups.Count).ResolveThirdPlaceQualifier(stage, combination); }
			catch (InvalidOperationException) { return null; }
		}
	}

	public override Team GetPlaceholder() => new() { Name = $"{FinalPlacement}. {Group?.Name ?? GroupId.ToString()}", ShortName = "TBD", Type = TeamType.PLACEHOLDER, };
}
