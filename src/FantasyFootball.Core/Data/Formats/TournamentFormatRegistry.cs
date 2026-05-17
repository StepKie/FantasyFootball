namespace FantasyFootball.Data.Formats;

/// <summary>
/// Resolves an <see cref="ITournamentFormat"/> from runtime context, used when the qualifier
/// machinery needs to advance third-place teams without holding a direct reference to the
/// competition factory that created the bracket.
/// </summary>
public static class TournamentFormatRegistry
{
	public static ITournamentFormat ForGroupCount(int groupCount) => groupCount switch
	{
		6 => EuroFormat.Instance,
		8 => WorldCupFormat.Instance,
		12 => ExpandedWorldCupFormat.Instance,
		_ => throw new NotSupportedException($"No tournament format registered for {groupCount} groups."),
	};
}
