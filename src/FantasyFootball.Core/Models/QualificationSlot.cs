namespace FantasyFootball.Models;

/// <summary>
/// Final-position consequence for a league standings row — UEFA berth, relegation, etc.
/// Drives the colored qualification bar on the standings table.
/// </summary>
public enum QualSlotKind
{
	CHAMPION,
	CHAMPIONS_LEAGUE,
	EUROPA_LEAGUE,
	CONFERENCE_LEAGUE,
	RELEGATION_PLAYOFF,
	RELEGATED,
}

/// <summary>
/// One band on the league standings table: the positions it covers and the kind of slot
/// (champion / European spot / relegation). Declared in the league definition JSON.
/// </summary>
public sealed record QualificationSlot(int[] Positions, QualSlotKind Kind);
