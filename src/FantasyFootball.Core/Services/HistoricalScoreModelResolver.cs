namespace FantasyFootball.Services;

/// <summary>
/// Resolves the <see cref="IScoreModel"/> a sim should use for a given
/// <see cref="Competition"/>. Priority: explicit <see cref="Competition.EloSetName"/>
/// (set via the setup picker) → year-matched EloSet (legacy fallback for
/// competitions saved before the picker) → null (caller uses the DI default,
/// i.e. the UI's active EloSet).
///
/// Returning <c>null</c> lets the call site fall through to its default model,
/// preserving the current "use whatever's active" behavior for competitions
/// whose year has no bundled EloSet (wm-2026 today).
/// </summary>
public static class HistoricalScoreModelResolver
{
	public static IScoreModel? Resolve(Competition competition, IRepository repo) =>
		Resolve(competition.EloSetName, competition.Year, repo);

	/// <summary>
	/// Scalar overload — lets callers skip materializing a Competition when all
	/// they need is the EloSet pick (e.g. <see cref="BulkSimRunner"/> on a
	/// RandomLineupSpec, where the full factory path would consume the draw
	/// algorithm's RNG as an unwanted side effect).
	/// </summary>
	public static IScoreModel? Resolve(string? eloSetName, int year, IRepository repo)
	{
		var sets = repo.GetAll<EloSet>();

		if (eloSetName is { } pinned)
		{
			var explicitSet = sets.FirstOrDefault(s => s.Name == pinned);
			if (explicitSet is not null) { return Build(explicitSet); }
			Log.Debug("Pinned EloSet {EloSetName} isn't in the repo; trying year-matched fallback", pinned);
		}

		var yearKey = year.ToString(CultureInfo.InvariantCulture);
		var yearSet = sets.FirstOrDefault(s => s.Name == yearKey);
		if (yearSet is not null) { return Build(yearSet); }

		Log.Debug("No EloSet named {Year}; falling back to active set", yearKey);
		return null;
	}

	static IScoreModel Build(EloSet set) => new EloScoreModel(new EloSetTeamRegistry(set));
}
