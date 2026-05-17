namespace FantasyFootball.Data.Formats;

/// <summary>
/// Describes the rules of a tournament's group stage and the bridge to the knockout bracket:
/// how many groups, how many teams per group, and which third-place finishers (if any) advance.
/// A format is independent of any specific *edition* (year) — Euro 2024 and Euro 2028 share one format.
/// </summary>
public interface ITournamentFormat
{
	int GroupCount { get; }
	int GroupSize { get; }

	/// <summary>Number of third-place finishers that advance to the knockout stage. 0 if none.</summary>
	int AdvancingThirdPlaceCount { get; }

	/// <summary>
	/// The slot constraint strings used by the bracket — each entry is a "/"-delimited list of
	/// group letters from which the third-place team for that knockout slot may come.
	/// Empty when no third-place teams advance.
	/// </summary>
	IReadOnlyList<string> ThirdPlaceSlotConstraints { get; }

	/// <summary>
	/// Resolves the team that fills a given third-place slot once the group stage is finished.
	/// </summary>
	/// <param name="groupStage">The completed group stage.</param>
	/// <param name="thirdPlaceSlot">One of <see cref="ThirdPlaceSlotConstraints"/>.</param>
	Team ResolveThirdPlaceQualifier(Stage groupStage, string thirdPlaceSlot);
}
