namespace FantasyFootball.Data;

public class TeamSelector : ITeamSelector
{
	readonly IList<Team> _pool;

	public TeamSelector(IEnumerable<Team> pool)
	{
		_pool = pool.ToList();
	}

	public Confederation ConfederationFilter { get; private set; }

	/// <summary> Return the specified number of teams sorted by elo, optionally filtered for the given confederation</summary>
	public List<Team> DrawTeamsWeightedByElo(int amount, Confederation? from = null)
	{
		var eligibleTeams = _pool.Where(t => from == null || t.Country.Confederation.Equals(from));
		var eligible = eligibleTeams.ToList();
		// Each team gets (team.elo - 1000 (but a minimum of 10)) balls into the drawing urn ...
		var urn = eligibleTeams.SelectMany(t => Enumerable.Repeat(t, Math.Max(t.Elo - 1000, 10))).ToList();
		// Order balls randomly, then remove duplicates and draw the desired amount
		var selected = urn
			.OrderBy(t => Guid.NewGuid())
			.Distinct()
			.Take(amount)
			.OrderBy(t => t.Elo)
			.ToList();

		return selected;
	}

	public IList<Team> GetTeams(int amount) => DrawTeamsWeightedByElo(amount, ConfederationFilter);
}
