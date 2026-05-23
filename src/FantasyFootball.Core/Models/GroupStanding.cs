namespace FantasyFootball.Models;

/// <summary>
/// One row of a group's standings table. Position is 1-based and
/// reflects sort order under FIFA rules (points → goal difference →
/// goals for → team-id alphabetical, the last as a deterministic
/// tiebreaker stand-in for head-to-head + fair-play + drawing of lots
/// — which we don't model yet).
/// </summary>
public sealed record class GroupStanding(
	string TeamId,
	int Played,
	int Wins,
	int Draws,
	int Losses,
	int GoalsFor,
	int GoalsAgainst,
	int Points,
	int Position)
{
	public int GoalDifference => GoalsFor - GoalsAgainst;
}
