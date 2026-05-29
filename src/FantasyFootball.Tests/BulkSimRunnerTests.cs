using System.Threading;
using FantasyFootball.Repositories;

namespace FantasyFootball.Tests;

/// <summary>
/// End-to-end bulk-sim integration: spec → factory → simulator → repo,
/// N times. Verifies every persisted run has full results and distinct
/// repo IDs.
/// </summary>
public class BulkSimRunnerTests
{
	readonly EmbeddedCompetitionDefinitionStore _definitions = new();
	readonly CompetitionFactory _factory;
	readonly CompetitionSimulator _simulator = new(new StubScoreModel());
	readonly InMemoryCompetitionRepository _repo = new();
	readonly InMemoryRepository _entityRepo = new();
	readonly BulkSimRunner _runner;

	public BulkSimRunnerTests()
	{
		_factory = new(_definitions);
		_runner = new(_factory, _simulator, _repo, _entityRepo);
	}

	[Fact]
	public async Task Run_Historical_PersistsRequestedCount()
	{
		var spec = new HistoricalSpec { DefinitionId = "wm-2022", Played = false };
		var ids = await _runner.RunAsync(spec, count: 3);

		ids.Should().HaveCount(3);
		ids.Distinct().Should().HaveCount(3, "each run gets a fresh ID");
		(await _repo.CountAsync()).Should().Be(3);
	}

	[Fact]
	public async Task Run_Historical_EachPersistedCompetitionIsFinished()
	{
		var spec = new HistoricalSpec { DefinitionId = "wm-2022", Played = false };
		var ids = await _runner.RunAsync(spec, count: 2);

		foreach (var id in ids)
		{
			var c = await _repo.GetAsync(id);
			c.Should().NotBeNull();
			c!.IsFinished().Should().BeTrue();
			c.SimulationStart.Should().NotBeNull();
			c.SimulationFinished.Should().NotBeNull();
		}
	}

	[Fact]
	public async Task Run_Historical_RunsAreIndependent()
	{
		// All Historical runs use the same lineup; with a deterministic
		// score model they SHOULD produce identical results too. The point
		// here is that they're stored as separate competition rows — not
		// that they're stored together.
		var spec = new HistoricalSpec { DefinitionId = "wm-2022", Played = false };
		var ids = await _runner.RunAsync(spec, count: 2);

		var first = await _repo.GetAsync(ids[0]);
		var second = await _repo.GetAsync(ids[1]);
		first!.Id.Should().NotBe(second!.Id);
	}

	[Fact]
	public async Task Run_RandomLineup_EachRunHasDifferentLineup()
	{
		// Always-Random mode: each iteration draws fresh teams.
		var teams = Enumerable.Range(1, 50).Select(i => $"T{i:00}").ToArray();
		var registry = new StubRegistry(teams);
		var draw = new UniformDrawFromRegistry(registry, new Random(42));
		var spec = new RandomLineupSpec { DefinitionId = "wm-2022", DrawAlgorithm = draw };

		var ids = await _runner.RunAsync(spec, count: 3);

		var competitions = new List<Competition>();
		foreach (var id in ids) { competitions.Add((await _repo.GetAsync(id))!); }
		var distinctLineups = competitions
			.Select(c => string.Join(",", c.GroupAssignments[0]))
			.Distinct()
			.Count();
		distinctLineups.Should().BeGreaterThan(1, "fresh draw per run should yield different group-A lineups");
	}

	[Fact]
	public async Task Run_CountZero_Throws()
	{
		var spec = new HistoricalSpec { DefinitionId = "wm-2022", Played = false };
		Func<Task> act = () => _runner.RunAsync(spec, 0);
		await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
	}

	[Fact]
	public async Task Run_ReportsProgressEveryIteration()
	{
		var spec = new HistoricalSpec { DefinitionId = "wm-2022", Played = false };
		var reports = new List<int>();
		var progress = new SyncProgress<int>(reports.Add);

		await _runner.RunAsync(spec, count: 5, progress);

		reports.Should().Equal(1, 2, 3, 4, 5);
	}

	[Fact]
	public async Task Run_HonorsCancellation_StopsMidway()
	{
		// Pre-cancelled token: not even the first iteration should run.
		var spec = new HistoricalSpec { DefinitionId = "wm-2022", Played = false };
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		Func<Task> act = () => _runner.RunAsync(spec, count: 10, cancellationToken: cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
		(await _repo.CountAsync()).Should().Be(0);
	}

	[Fact]
	public async Task Run_Historical_UsesYearMatchedEloSetOverride()
	{
		// Throwing default model verifies the year-matched EloSet override is the sole score path.
		var throwingRunner = new BulkSimRunner(
			_factory,
			new CompetitionSimulator(new ThrowOnCallScoreModel()),
			_repo,
			_entityRepo);

		string[] wm2018Teams = ["ARG","AUS","BEL","BRA","COL","CRC","CRO","DEN","EGY","ENG","ESP","FRA","GER","IRN","ISL","JPN","KOR","KSA","MAR","MEX","NGA","PAN","PER","POL","POR","RUS","SEN","SRB","SUI","SWE","TUN","URU"];
		_entityRepo.Save(new EloSet
		{
			Name = "2018",
			Date = new DateOnly(2018, 6, 14),
			Snapshot = wm2018Teams.ToDictionary(t => t, _ => 1500),
		});

		var spec = new HistoricalSpec { DefinitionId = "wm-2018", Played = false };
		Func<Task> act = () => throwingRunner.RunAsync(spec, count: 1);

		await act.Should().NotThrowAsync("the year-matched override should supply every score, never falling back to the throwing default");
	}

	sealed class ThrowOnCallScoreModel : IScoreModel
	{
		public Result ScoreGroupGame(string h, string a) => throw new InvalidOperationException("default model called — override didn't kick in");
		public Result ScoreKoGame(string h, string a) => throw new InvalidOperationException("default model called — override didn't kick in");
	}

	sealed class StubRegistry : ITeamRegistry
	{
		public IReadOnlyList<string> AllTeamIds { get; }
		public StubRegistry(IReadOnlyList<string> teams) { AllTeamIds = teams; }
		public int EloOf(string teamId) => 1500;     // uniform ELO; bulk-sim runner doesn't use this directly
	}

	// Synchronous IProgress<T> so progress assertions don't race the sync-context drain.
	sealed class SyncProgress<T> : IProgress<T>
	{
		readonly Action<T> _callback;
		public SyncProgress(Action<T> callback) { _callback = callback; }
		public void Report(T value) => _callback(value);
	}
}
