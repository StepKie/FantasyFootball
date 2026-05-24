using FantasyFootball.Data;

namespace FantasyFootball.Models;

/// <summary>
/// Outcome of a played Game. Value type — lives inline in the Game
/// (no heap allocation per played game). Presence/absence on
/// <see cref="Game.Result"/> is the single source of truth for
/// played-vs-scheduled state.
///
/// For <see cref="GameEnd.PENALTIES"/>, <see cref="HomeScore"/> and
/// <see cref="AwayScore"/> are the regulation + extra-time score
/// (always equal — that's why it went to penalties). The shootout
/// score is in <see cref="PenaltyHome"/> / <see cref="PenaltyAway"/>.
///
/// <see cref="ToString"/> is the record default (verbose,
/// repr-like — useful in debugger / test failure output).
/// <see cref="Format"/> is the compact console form (<c>"2-1"</c>,
/// <c>"2-1 e.t."</c>, <c>"1-1 (5-4 pen.)"</c>).
/// </summary>
public readonly record struct Result(int HomeScore, int AwayScore, GameEnd Ending, int PenaltyHome = 0, int PenaltyAway = 0)
{
	/// <summary>Compact console form, e.g. <c>"2-1"</c>, <c>"2-1 e.t."</c>, or <c>"1-1 (5-4 pen.)"</c>.</summary>
	public string Format() => Ending switch
	{
		GameEnd.EXTRA_TIME => $"{HomeScore}-{AwayScore} e.t.",
		GameEnd.PENALTIES => $"{HomeScore}-{AwayScore} ({PenaltyHome}-{PenaltyAway} pen.)",
		_ => $"{HomeScore}-{AwayScore}",
	};

	/// <summary>True if the home side won — for PENALTIES, compares the shootout score (regulation is always tied).</summary>
	public bool HomeWon => Ending == GameEnd.PENALTIES ? PenaltyHome > PenaltyAway : HomeScore > AwayScore;
	/// <summary>True if the away side won — for PENALTIES, compares the shootout score.</summary>
	public bool AwayWon => Ending == GameEnd.PENALTIES ? PenaltyAway > PenaltyHome : AwayScore > HomeScore;
	/// <summary>True only for group games ending in regulation with equal scores. KO endings (ET, PENALTIES) always have a winner.</summary>
	public bool IsDraw => Ending == GameEnd.NORMAL && HomeScore == AwayScore;
}
