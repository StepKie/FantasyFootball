namespace FantasyFootball.Services;

/// <summary>
/// Looks up the <see cref="EloSet"/> / <see cref="IScoreModel"/> a sim should use for a given
/// <see cref="Competition"/> by its pinned <see cref="Competition.EloSetName"/>. Null EloSetName
/// or unresolvable name returns null — callers decide whether that's an error.
/// </summary>
public static class HistoricalScoreModelResolver
{
	public static IScoreModel? Resolve(Competition competition, IRepository repo) =>
		Resolve(competition.EloSetName, repo);

	public static IScoreModel? Resolve(string? eloSetName, IRepository repo)
	{
		var set = ResolveEloSet(eloSetName, repo);
		return set is null ? null : new EloScoreModel(new EloSetTeamRegistry(set));
	}

	public static EloSet? ResolveEloSet(string? eloSetName, IRepository repo) =>
		eloSetName is null ? null : repo.GetAll<EloSet>().FirstOrDefault(s => s.Name == eloSetName);
}
