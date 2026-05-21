using FantasyFootball.Models;
using FantasyFootball.Repositories;

namespace FantasyFootball.Services;

/// <summary>
/// Runs the bulk-simulation flow: a <see cref="CompetitionSpec"/> in,
/// N persisted, fully-simulated <see cref="FlatCompetition"/>s out.
///
/// One loop iteration per requested run:
/// <list type="number">
///   <item><see cref="FlatCompetitionFactory"/> turns the spec into a
///         fresh competition (Historical = same lineup each run;
///         CustomLineup = same fixed lineup; RandomLineup = fresh
///         draw per iteration).</item>
///   <item><see cref="FlatCompetitionSimulator"/> walks the games and
///         fills in Results.</item>
///   <item><see cref="IFlatCompetitionRepository.SaveAsync"/> persists
///         it under a fresh ID.</item>
/// </list>
///
/// Returns the IDs of every persisted run, in order, so the caller
/// can route the UI to the aggregator / browse list.
/// </summary>
public sealed class BulkSimRunner
{
	readonly FlatCompetitionFactory _factory;
	readonly FlatCompetitionSimulator _simulator;
	readonly IFlatCompetitionRepository _repo;

	public BulkSimRunner(
		FlatCompetitionFactory factory,
		FlatCompetitionSimulator simulator,
		IFlatCompetitionRepository repo)
	{
		_factory = factory;
		_simulator = simulator;
		_repo = repo;
	}

	public async Task<IReadOnlyList<int>> RunAsync(CompetitionSpec spec, int count)
	{
		if (count < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(count), count, "Bulk sim needs at least one run.");
		}

		var ids = new List<int>(count);
		for (int i = 0; i < count; i++)
		{
			var competition = _factory.Create(spec);
			_simulator.Simulate(competition);
			var id = await _repo.SaveAsync(competition);
			ids.Add(id);
		}
		return ids;
	}
}
