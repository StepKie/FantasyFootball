namespace FantasyFootball.Services;

/// <summary>
/// Compact 2–3 character codes for round IDs — "R16" / "QF" / "SF" / "F"
/// instead of the full localized names ("Round of 16", "Quarterfinal").
/// Used in:
/// <list type="bullet">
///   <item>Qualifier slot labels ("Winner of R16 #1")</item>
///   <item>Game-row group tag ("R16 1", "QF 2") for KO games where the
///         group letter doesn't apply.</item>
/// </list>
/// Keyed by the round's stable <c>FlatRound.Id</c> slug; falls back to
/// the supplied id verbatim if not mapped.
/// </summary>
public static class FlatRoundShortNames
{
	static readonly Dictionary<string, string> Map = new()
	{
		["r64"] = "R64",
		["r32"] = "R32",
		["r16"] = "R16",
		["qf"] = "QF",
		["sf"] = "SF",
		["third"] = "3rd",
		["final"] = "F",
		["group-r1"] = "MD1",
		["group-r2"] = "MD2",
		["group-r3"] = "MD3",
	};

	public static string Of(string roundId) =>
		Map.TryGetValue(roundId, out var s) ? s : roundId;
}
