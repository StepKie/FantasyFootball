namespace FantasyFootball.Services;

public static class IActiveEloSetExtensions
{
	/// <summary>Elo of the given FIFA 3-letter code in the active snapshot, or 0 if no active set / not in the snapshot.</summary>
	public static int EloOf(this IActiveEloSet active, string code3) =>
		active.Current?.Snapshot.GetValueOrDefault(code3) ?? 0;

	/// <summary>Convenience overload: looks up by <see cref="Team.ShortName"/>. Returns 0 for a null team.</summary>
	public static int EloOf(this IActiveEloSet active, Team? team) =>
		team is null ? 0 : active.EloOf(team.ShortName);
}
