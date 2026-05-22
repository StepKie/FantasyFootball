namespace FantasyFootball.Models;

/// <summary>
/// Parsed form of a knockout-game qualifier expression. The DSL strings
/// stored on <see cref="FlatKoGame.HomeQual"/> / <see cref="FlatKoGame.AwayQual"/>
/// are parsed into one of these typed forms at resolution time.
///
/// DSL forms (case-sensitive; uppercase letters):
/// <list type="bullet">
///   <item><c>A1</c>, <c>A2</c>, <c>B3</c>, <c>L1</c> — single-group placement
///         (<see cref="FlatGroupPlacement"/>). No dash.</item>
///   <item><c>W-49</c> — winner of game ID 49
///         (<see cref="FlatGameWinner"/>). Dash after the W.</item>
///   <item><c>L-61</c> — loser of game ID 61
///         (<see cref="FlatGameLoser"/>). Dash after the L; disambiguates
///         from group L placement (e.g. <c>L1</c> is group L 1st place,
///         <c>L-1</c> would be loser of game 1).</item>
///   <item><c>A/B/F/G/I3</c> — best 3rd-place finisher among the listed
///         groups (<see cref="FlatThirdPlacePool"/>). Letters separated
///         by <c>/</c>, ending with <c>3</c>.</item>
/// </list>
///
/// `Flat` prefix is transient (see <see cref="FlatStage"/>).
/// </summary>
public abstract record class FlatQualifier
{
	/// <summary>The DSL string this qualifier round-trips through.</summary>
	public abstract string Source { get; }
}

/// <summary>"A1" / "A2" / "L1" — n-th place from a specific group.</summary>
public sealed record class FlatGroupPlacement(string GroupLetter, int Place) : FlatQualifier
{
	public override string Source => $"{GroupLetter}{Place}";
}

/// <summary>"W-49" — winner of game with the given Id.</summary>
public sealed record class FlatGameWinner(int GameId) : FlatQualifier
{
	public override string Source => $"W-{GameId}";
}

/// <summary>"L-61" — loser of game with the given Id (e.g. third-place match).</summary>
public sealed record class FlatGameLoser(int GameId) : FlatQualifier
{
	public override string Source => $"L-{GameId}";
}

/// <summary>"A/B/F/G/I3" — best 3rd-place finisher among the listed groups.</summary>
public sealed record class FlatThirdPlacePool(IReadOnlyList<string> EligibleGroups) : FlatQualifier
{
	public override string Source => $"{string.Join("/", EligibleGroups)}3";
}
