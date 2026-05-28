using FantasyFootball.Models;
using FantasyFootball.Repositories;

namespace FantasyFootball.Services;

/// <summary>
/// Runs the bulk-simulation flow: a <see cref="CompetitionSpec"/> in,
/// N persisted, fully-simulated <see cref="Competition"/>s out.
///
/// One loop iteration per requested run:
/// <list type="number">
///   <item><see cref="CompetitionFactory"/> turns the spec into a
///         fresh competition (Historical = same lineup each run;
///         CustomLineup = same fixed lineup; RandomLineup = fresh
///         draw per iteration).</item>
///   <item><see cref="CompetitionSimulator"/> walks the games and
///         fills in Results.</item>
///   <item><see cref="ICompetitionRepository.SaveAsync"/> persists
///         it under a fresh ID.</item>
/// </list>
///
/// Returns the IDs of every persisted run, in order, so the caller
/// can route the UI to the aggregator / browse list.
/// </summary>
public sealed class BulkSimRunner
{
	readonly CompetitionFactory _factory;
	readonly CompetitionSimulator _simulator;
	readonly ICompetitionRepository _repo;
	readonly IRepository _entityRepo;

	public BulkSimRunner(
		CompetitionFactory factory,
		CompetitionSimulator simulator,
		ICompetitionRepository repo,
		IRepository entityRepo)
	{
		_factory = factory;
		_simulator = simulator;
		_repo = repo;
		_entityRepo = entityRepo;
	}

	public async Task<IReadOnlyList<int>> RunAsync(
		CompetitionSpec spec,
		int count,
		IProgress<int>? progress = null,
		CancellationToken cancellationToken = default)
	{
		if (count < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(count), count, "Bulk sim needs at least one run.");
		}

		// HistoricalSpec sims read elos from the year-matched EloSet instead of the
		// user's UI-active set; the lookup is once per run since the snapshot doesn't
		// change mid-bulk.
		IScoreModel? historicalOverride = ResolveHistoricalScoreModel(spec);

		var ids = new List<int>(count);
		for (int i = 0; i < count; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var competition = _factory.Create(spec);
			_simulator.Simulate(competition, historicalOverride);
			var id = await _repo.SaveAsync(competition);
			ids.Add(id);
			progress?.Report(i + 1);
			// Yield so the render loop can paint the progress bar between runs.
			await Task.Yield();
		}

		return ids;
	}

	IScoreModel? ResolveHistoricalScoreModel(CompetitionSpec spec)
	{
		if (spec is not HistoricalSpec) { return null; }
		// Materialize the spec once to read its Year; cheap relative to the upcoming N runs.
		var sample = _factory.Create(spec);
		var year = sample.Year.ToString(CultureInfo.InvariantCulture);
		var historical = _entityRepo.GetAll<EloSet>().FirstOrDefault(s => s.Name == year);
		return historical is null ? null : new EloScoreModel(new EloSetTeamRegistry(historical));
	}
}
