using System.Diagnostics;

namespace FantasyFootball.Tests;

/// <summary>
/// Pure timing harness for full-competition simulation. Reports wall-clock cost of
/// <see cref="CompetitionSimulator.Simulate"/> per Competition type so we can see
/// whether changes to the sim or persistence layer regress hot-path latency.
///
/// Not an assertion test — failures here would be a perf regression, but the
/// numbers are emitted to test output for human inspection rather than gated.
/// </summary>
public class SimulationBenchmarkTests(ITestOutputHelper output) : BaseTest(output, level: LogEventLevel.Warning)
{
	[InlineData(CompetitionType.EM, 2024, "EM24 (24 teams, 51 games)")]
	[InlineData(CompetitionType.WM, 2022, "WC32 (32 teams, 64 games)")]
	[InlineData(CompetitionType.WM, 2026, "WC48 (48 teams, 104 games)")]
	[Theory]
	public async Task TimeFullCompetitionSimulation(CompetitionType type, int year, string label)
	{
		const int Runs = 5;
		var timings = new long[Runs];

		for (int i = 0; i < Runs; i++)
		{
			var competition = InitCompetition(type, year);
			var simulator = new CompetitionSimulator(competition, Repo);

			try
			{
				var sw = Stopwatch.StartNew();
				await simulator.Simulate();
				sw.Stop();
				timings[i] = sw.ElapsedMilliseconds;
			}
			finally
			{
				// Ensure the in-memory DB doesn't accumulate stale Competitions if a sim throws
				// — later iterations would otherwise pay extra GetAllWithChildren cost.
				Repo.Delete(competition);
			}
		}

		var totalGames = type switch
		{
			CompetitionType.EM => 51,
			CompetitionType.WM when year == 2026 => 104,
			CompetitionType.WM => 64,
			_ => 0,
		};

		var min = timings.Min();
		var max = timings.Max();
		var avg = timings.Average();
		var perGame = totalGames > 0 ? avg / totalGames : 0;

		Output.WriteLine($"{label}");
		Output.WriteLine($"  Runs: {Runs}");
		Output.WriteLine($"  Total competition: min {min} ms / avg {avg:F1} ms / max {max} ms");
		Output.WriteLine($"  Per game:          avg {perGame:F2} ms");
		Output.WriteLine($"  Raw timings (ms):  [{string.Join(", ", timings)}]");
	}

	[InlineData(CompetitionType.EM, 2024, "EM24 (24 teams, 51 games)")]
	[InlineData(CompetitionType.WM, 2022, "WC32 (32 teams, 64 games)")]
	[InlineData(CompetitionType.WM, 2026, "WC48 (48 teams, 104 games)")]
	[Theory]
	public void TimeInitCompetition(CompetitionType type, int year, string label)
	{
		// First call warms the CSV cache; not part of the steady-state measurement.
		_ = InitCompetition(type, year);
		Repo.Reset();

		const int Runs = 5;
		var factoryCreate = new long[Runs];
		var factoryToCompetition = new long[Runs];
		var repoSave = new long[Runs];
		var repoGet = new long[Runs];

		for (int i = 0; i < Runs; i++)
		{
			var sw = Stopwatch.StartNew();
			var factory = CompetitionFactory.Default(type, DataService, year);
			sw.Stop(); factoryCreate[i] = sw.ElapsedMilliseconds;

			sw.Restart();
			var competition = factory.Create();
			sw.Stop(); factoryToCompetition[i] = sw.ElapsedMilliseconds;

			try
			{
				sw.Restart();
				Repo.Save(competition);
				sw.Stop(); repoSave[i] = sw.ElapsedMilliseconds;

				sw.Restart();
				_ = Repo.GetAll<Competition>();
				sw.Stop(); repoGet[i] = sw.ElapsedMilliseconds;
			}
			finally
			{
				// Mirror the cleanup guarantee from TimeFullCompetitionSimulation — a Repo.Save
				// or Repo.GetAll throw would otherwise leak stale rows into later iterations and
				// distort the GetAll measurement.
				Repo.Delete(competition);
			}
		}

		Output.WriteLine($"{label}");
		Output.WriteLine($"  Runs: {Runs} (after 1 warmup)");
		Output.WriteLine($"  CompetitionFactory.Default():  avg {factoryCreate.Average():F1} ms  raw {Fmt(factoryCreate)}");
		Output.WriteLine($"  factory.Create():              avg {factoryToCompetition.Average():F1} ms  raw {Fmt(factoryToCompetition)}");
		Output.WriteLine($"  Repo.Save(competition):        avg {repoSave.Average():F1} ms  raw {Fmt(repoSave)}");
		Output.WriteLine($"  Repo.GetAll<Competition>():    avg {repoGet.Average():F1} ms  raw {Fmt(repoGet)}");
		var totalAvg = factoryCreate.Average() + factoryToCompetition.Average() + repoSave.Average();
		Output.WriteLine($"  ----------------");
		Output.WriteLine($"  Total InitCompetition (avg):   {totalAvg:F1} ms");
	}

	static string Fmt(long[] xs) => $"[{string.Join(", ", xs)}]";
}
