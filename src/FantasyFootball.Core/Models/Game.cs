using System.Text.Json.Serialization;

namespace FantasyFootball.Models;

/// <summary>
/// Sealed hierarchy on game KIND (Group vs KO). The two-state lifecycle
/// (scheduled vs played) is orthogonal — captured by nullable
/// <see cref="Result"/> on the base.
///
/// JSON polymorphism uses a "kind" discriminator: "group" or "ko".
///
/// <c>ToString()</c> stays as the record default (verbose, repr-like —
/// useful in debugger / test failure output). For compact console output,
/// call <see cref="Format"/> on the subclass.
///
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(GroupGame), "group")]
[JsonDerivedType(typeof(KoGame), "ko")]
public abstract record class Game
{
	/// <summary>
	/// Explicit ID, set in the competition-definition JSON. Referenced by
	/// qualifier strings (e.g. <c>W49</c> = winner of the game with Id=49).
	/// </summary>
	public required int Id { get; init; }

	public required DateTime PlayedOn { get; init; }

	/// <summary>FK into <see cref="Competition.Rounds"/>. Stage is implied via Round.StageId.</summary>
	public required string RoundId { get; init; }

	/// <summary>FK into the global Venue registry. Optional.</summary>
	public string? VenueId { get; init; }

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
	public required string HomeTeamId { get; init; }
	public required string AwayTeamId { get; init; }

	public override string Format() =>
		Result is { } r
			? $"[{Id}] {PlayedOn:dd.MM.yyyy HH:mm} {HomeTeamId} {r.Format()} {AwayTeamId}  [Group {GroupLetter} / {RoundId}]"
			: $"[{Id}] {PlayedOn:dd.MM.yyyy HH:mm} {HomeTeamId} v {AwayTeamId}  [Group {GroupLetter} / {RoundId}]";
}

/// <summary>
/// Knockout-stage game. Teams come from a qualifier expression evaluated
/// against earlier stage outcomes; the resolved team IDs are cached on
/// <see cref="HomeTeamId"/> / <see cref="AwayTeamId"/> once the upstream
/// qualifier resolves, so subsequent reads don't re-walk the qualifier
/// chain.
///
/// Qualifier DSL: <c>A1</c> = group A's 1st place, <c>W49</c> = winner
/// of game 49, <c>L61</c> = loser of game 61, <c>A/B/F3</c> = best
/// 3rd-place finisher among the listed groups.
/// </summary>
public sealed record class KoGame : Game
{
	public required string HomeQual { get; init; }
	public required string AwayQual { get; init; }

	/// <summary>
	/// Cached resolution of <see cref="HomeQual"/>. Null until the
	/// upstream qualifier becomes computable; written when resolved.
	/// </summary>
	public string? HomeTeamId { get; set; }

	/// <summary>
	/// Cached resolution of <see cref="AwayQual"/>.
	/// </summary>
	public string? AwayTeamId { get; set; }

	public override string Format()
	{
		// Show resolved team IDs once we have them; fall back to the
		// qualifier expression so the line is still meaningful for an
		// unresolved KO slot (e.g. before the group stage finishes).
		var home = HomeTeamId ?? HomeQual;
		var away = AwayTeamId ?? AwayQual;
		return Result is { } r
			? $"[{Id}] {PlayedOn:dd.MM.yyyy HH:mm} {home} {r.Format()} {away}  [{RoundId}]"
			: $"[{Id}] {PlayedOn:dd.MM.yyyy HH:mm} {home} v {away}  [{RoundId}]";
	}
}
