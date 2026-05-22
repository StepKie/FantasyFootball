using FantasyFootball.Data;

namespace FantasyFootball.Models;

/// <summary>
/// Outcome of a played FlatGame. Value type — lives inline in the Game
/// (no heap allocation per played game). Presence/absence on
/// <see cref="FlatGame.Result"/> is the single source of truth for
/// played-vs-scheduled state.
///
/// <see cref="ToString"/> is the record default (verbose,
/// repr-like — useful in debugger / test failure output).
/// <see cref="Format"/> is the compact console form (<c>"2-1"</c>,
/// <c>"2-1 e.t."</c>).
///
/// Future expansion (not in this PR): Attendance, Goal[], Card[], etc.
/// New fields are additive — older serialized competitions deserialize
/// fine with default values.
///
/// `Flat` prefix is transient (see <see cref="FlatStage"/>).
/// </summary>
public readonly record struct FlatResult(int HomeScore, int AwayScore, GameEnd Ending)
{
	/// <summary>Compact console form, e.g. <c>"2-1"</c> or <c>"2-1 e.t."</c>.</summary>
	public string Format() => Ending switch
	{
		GameEnd.EXTRA_TIME => $"{HomeScore}-{AwayScore} e.t.",
		GameEnd.PENALTIES => $"{HomeScore}-{AwayScore} pen.",
		_ => $"{HomeScore}-{AwayScore}",
	};
}
