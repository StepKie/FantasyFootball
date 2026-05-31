using System.Text.Json.Serialization;

namespace FantasyFootball.Models;

/// <summary>
/// Sealed hierarchy on game KIND (group, league, KO). The two-state lifecycle
/// (scheduled vs played) is orthogonal — captured by nullable <see cref="Result"/>
/// on the base.
///
/// <see cref="HomeTeamId"/> / <see cref="AwayTeamId"/> live on the base so callers
/// can read them uniformly. Direct games (Group/League) populate them at definition
/// time; KO games leave them null until the qualifier chain resolves, then write the
/// resolved id in.
///
/// JSON polymorphism uses a "kind" discriminator: "group", "league", or "ko".
///
/// <c>ToString()</c> stays as the record default (verbose, repr-like — useful in
/// debugger / test failure output). For compact console output, call <see cref="Format"/>
/// on the subclass.
///
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(GroupGame), "group")]
[JsonDerivedType(typeof(KoGame), "ko")]
[JsonDerivedType(typeof(LeagueGame), "league")]
public abstract record class Game
{
	/// <summary>
	/// Explicit ID, set in the competition-definition JSON. Referenced by
	/// qualifier strings (e.g. <c>W-49</c> = winner of the game with Id=49).
	/// </summary>
	public required int Id { get; init; }

	public required DateTime PlayedOn { get; init; }

	/// <summary>FK into <see cref="Competition.Rounds"/>. Stage is implied via Round.StageId.</summary>
	public required string RoundId { get; init; }

	/// <summary>FK into the global Venue registry. Optional.</summary>
	public string? VenueId { get; init; }

	/// <summary>Official spectator count for completed real-world games. Null for simulated or unplayed matches.</summary>
	public int? Attendance { get; init; }

	/// <summary>
	/// Home team id. Set at definition time for Group/League games (where the lineup is
	/// known up front) and at qualifier-resolution time for <see cref="KoGame"/>. Null only
	/// before a KO qualifier resolves; the loader rejects null for Group/League games.
	/// </summary>
	public string? HomeTeamId { get; set; }

	/// <summary>Away team id — same semantics as <see cref="HomeTeamId"/>.</summary>
	public string? AwayTeamId { get; set; }

	/// <summary>
	/// null = scheduled (not yet played). Non-null = played.
	/// </summary>
	public Result? Result { get; set; }

	/// <summary>
	/// Compact console-friendly format (the str() equivalent). Subclasses
	/// override; the default ToString stays as the auto-generated record
	/// repr() form for debugger / test failure output.
	/// </summary>
	public abstract string Format();
}

/// <summary>
/// Group-stage game. Teams are known at competition start (assigned to
/// the group letter via Competition.GroupAssignments).
/// </summary>
public sealed record class GroupGame : Game
{
	public required string GroupLetter { get; init; }

	public override string Format() =>
		Result is { } r
			? $"[{Id}] {PlayedOn:dd.MM.yyyy HH:mm} {HomeTeamId} {r.Format()} {AwayTeamId}  [Group {GroupLetter} / {RoundId}]"
			: $"[{Id}] {PlayedOn:dd.MM.yyyy HH:mm} {HomeTeamId} v {AwayTeamId}  [Group {GroupLetter} / {RoundId}]";
}

/// <summary>
/// League round-robin game. Teams are known up front (from <see cref="Competition.Teams"/>);
/// there's no group letter — leagues don't have a group concept (groups exist only in
/// cup formats as a precursor to a knockout/intermediate phase).
/// </summary>
public sealed record class LeagueGame : Game
{
	public override string Format() =>
		Result is { } r
			? $"[{Id}] {PlayedOn:dd.MM.yyyy HH:mm} {HomeTeamId} {r.Format()} {AwayTeamId}  [{RoundId}]"
			: $"[{Id}] {PlayedOn:dd.MM.yyyy HH:mm} {HomeTeamId} v {AwayTeamId}  [{RoundId}]";
}

/// <summary>
/// Knockout-stage game. Teams come from a qualifier expression evaluated
/// against earlier stage outcomes; the resolved team IDs are cached on
/// <see cref="Game.HomeTeamId"/> / <see cref="Game.AwayTeamId"/> once the upstream
/// qualifier resolves, so subsequent reads don't re-walk the qualifier chain.
///
/// Qualifier DSL: <c>A1</c> = group A's 1st place, <c>W-49</c> = winner
/// of game 49, <c>L-61</c> = loser of game 61, <c>A/B/F3</c> = best
/// 3rd-place finisher among the listed groups.
/// </summary>
public sealed record class KoGame : Game
{
	public required string HomeQual { get; init; }
	public required string AwayQual { get; init; }

	public override string Format()
	{
		// Resolved IDs take priority; fall back to the qualifier expression for unresolved KO slots.
		var home = HomeTeamId ?? HomeQual;
		var away = AwayTeamId ?? AwayQual;
		return Result is { } r
			? $"[{Id}] {PlayedOn:dd.MM.yyyy HH:mm} {home} {r.Format()} {away}  [{RoundId}]"
			: $"[{Id}] {PlayedOn:dd.MM.yyyy HH:mm} {home} v {away}  [{RoundId}]";
	}
}
