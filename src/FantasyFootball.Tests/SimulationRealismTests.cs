using Xunit;

namespace FantasyFootball.Tests;

/// <summary>
/// Monte-Carlo realism check on EloScoreModel. Runs N matches at various
/// Elo gaps and dumps aggregate stats — goals per game, score distribution,
/// KO extra-time / penalty fractions — to xUnit test output so the values
/// can be eyeballed against real-world WC / EM benchmarks.
/// </summary>
public class SimulationRealismTests
{
	readonly ITestOutputHelper _out;
	public SimulationRealismTests(ITestOutputHelper out_) { _out = out_; }

	sealed class FixedRegistry : ITeamRegistry
	{
		readonly Dictionary<string, int> _elo;
		public FixedRegistry(params (string id, int elo)[] e) => _elo = e.ToDictionary(t => t.id, t => t.elo);
		public IReadOnlyList<string> AllTeamIds => [.. _elo.Keys];
		public int EloOf(string id) => _elo[id];
	}

	[Fact]
	public void Report_GoalsPerGame_AcrossEloGaps()
	{
		// Sanity bounds — assert each case stays in a plausible football range so a broken sampler trips this test.
		double minGoals = double.MaxValue, maxGoals = double.MinValue;
		const int N = 10_000;
		// Calibrated to current FIFA Elo bands (post-2026 refresh).
		var cases = new (string label, int homeElo, int awayElo)[]
		{
			("Top-tier vs Top-tier  (gap   0)", 2050, 2050),     // Spain vs Argentina
			("Top-tier vs Mid       (gap +400)", 2050, 1650),    // Spain vs Wales/Slovakia
			("Strong  vs Strong    (gap   0)", 1750, 1750),      // Germany-tier
			("Strong  vs Weak      (gap +500)", 1750, 1250),     // Germany vs Honduras
			("Equal mid           (gap   0)", 1600, 1600),       // Greece vs Hungary
			("Massive mismatch    (gap +800)", 2050, 1250),      // Spain vs Honduras
		};

		_out.WriteLine($"=== Goals/game over {N} sims per case ===");
		_out.WriteLine("  Case                                      Total  Home   Away   Home Win%  Draw%  Away Win%");
		foreach (var (label, eH, eA) in cases)
		{
			var registry = new FixedRegistry(("H", eH), ("A", eA));
			var model = new EloScoreModel(registry, new Random(42));
			int tg = 0, hg = 0, ag = 0, hw = 0, d = 0, aw = 0;
			for (int i = 0; i < N; i++)
			{
				var r = model.ScoreGroupGame("H", "A");
				tg += r.HomeScore + r.AwayScore; hg += r.HomeScore; ag += r.AwayScore;
				if (r.HomeWon) { hw++; }
				else if (r.AwayWon) { aw++; }
				else { d++; }
			}
			var avg = tg / (double)N;
			minGoals = Math.Min(minGoals, avg);
			maxGoals = Math.Max(maxGoals, avg);
			_out.WriteLine($"  {label,-40}  {avg,5:F2}  {hg / (double)N,5:F2}  {ag / (double)N,5:F2}  {100.0 * hw / N,8:F1}%  {100.0 * d / N,5:F1}%  {100.0 * aw / N,8:F1}%");
		}
		minGoals.Should().BeGreaterThan(1.5, "no realistic case should drop below 1.5 goals/game");
		maxGoals.Should().BeLessThan(4.0, "even +800 Elo gaps shouldn't exceed ~3.5 goals/game in this model");
		_out.WriteLine("");
		_out.WriteLine("  REAL-WORLD REFERENCE:");
		_out.WriteLine("    WC 2018: 2.64 goals/game across 64 games");
		_out.WriteLine("    WC 2022: 2.69 goals/game across 64 games");
		_out.WriteLine("    Top-vs-bottom group games at the WC often see 4-7 goal margins (e.g. Argentina 5-0 Croatia in semi was an outlier).");
	}

	[Fact]
	public void Report_ScoreDistribution_EqualElos()
	{
		const int N = 20_000;
		var registry = new FixedRegistry(("H", 1700), ("A", 1700));
		var model = new EloScoreModel(registry, new Random(42));
		var counts = new Dictionary<string, int>();
		foreach (var k in new[] { "0-0", "1-0", "0-1", "1-1", "2-0", "0-2", "2-1", "1-2", "2-2", "3-0", "0-3", "3+/≤2 (H)", "≤2/3+ (A)", "3+/3+" })
		{
			counts[k] = 0;
		}
		for (int i = 0; i < N; i++)
		{
			var r = model.ScoreGroupGame("H", "A");
			var h = r.HomeScore; var a = r.AwayScore;
			var key = (h, a) switch
			{
				(0, 0) => "0-0",
				(1, 0) => "1-0",
				(0, 1) => "0-1",
				(1, 1) => "1-1",
				(2, 0) => "2-0",
				(0, 2) => "0-2",
				(2, 1) => "2-1",
				(1, 2) => "1-2",
				(2, 2) => "2-2",
				(3, 0) => "3-0",
				(0, 3) => "0-3",
				(>= 3, _) when a <= 2 => "3+/≤2 (H)",
				(_, >= 3) when h <= 2 => "≤2/3+ (A)",
				_ => "3+/3+",
			};
			counts[key]++;
		}
		_out.WriteLine($"=== Score distribution over {N} equal-Elo sims (Elo=1700 both sides) ===");
		foreach (var (k, v) in counts.OrderByDescending(p => p.Value))
		{
			_out.WriteLine($"  {k,-7}  {100.0 * v / N,5:F1}%  ({v})");
		}
		// Sanity bounds against real top-flight football frequencies.
		(counts["0-0"] / (double)N).Should().BeInRange(0.04, 0.12, "0-0 rate should be in the real-world 4-12% band");
		(counts["1-1"] / (double)N).Should().BeInRange(0.07, 0.18, "1-1 rate should be in the real-world 7-18% band");
		_out.WriteLine("");
		_out.WriteLine("  REAL-WORLD REFERENCE (top-flight football, all matches):");
		_out.WriteLine("    1-1: ~11%   1-0: ~10%   0-1: ~9%   2-1: ~9%   2-0: ~7%   0-0: ~7-8%");
	}

	[Fact]
	public void Report_KoEndings_AndExtraTimeRate()
	{
		const int N = 5_000;
		_out.WriteLine($"=== KO endings over {N} sims per case ===");
		_out.WriteLine("  Case                          Normal%   ET%    Pens%");
		var cases = new (string label, int h, int a)[]
		{
			("Equal Elo (1700 vs 1700)", 1700, 1700),
			("Slight edge (+200)     ", 1700, 1500),
			("Big edge (+500)        ", 1700, 1200),
		};
		var equalNormalRate = -1.0;
		foreach (var (label, eH, eA) in cases)
		{
			var registry = new FixedRegistry(("H", eH), ("A", eA));
			var model = new EloScoreModel(registry, new Random(42));
			int normal = 0, et = 0, pen = 0;
			for (int i = 0; i < N; i++)
			{
				var r = model.ScoreKoGame("H", "A");
				switch (r.Ending) { case GameEnd.NORMAL: normal++; break; case GameEnd.EXTRA_TIME: et++; break; case GameEnd.PENALTIES: pen++; break; }
			}
			_out.WriteLine($"  {label,-30} {100.0 * normal / N,6:F1}% {100.0 * et / N,5:F1}% {100.0 * pen / N,6:F1}%");
			if (eH == eA) { equalNormalRate = normal / (double)N; }
		}
		equalNormalRate.Should().BeInRange(0.50, 0.85, "equal-Elo KO games should mostly finish in regular time but with a meaningful tiebreaker fraction");
		_out.WriteLine("");
		_out.WriteLine("  REAL-WORLD REFERENCE:");
		_out.WriteLine("    Recent WC KO rounds: ~22% go to ET, ~10-12% to penalties.");
		_out.WriteLine("    For lopsided matchups (e.g. top-seed vs round-of-16 7-seed), ET/pen rate is much lower in reality (~10% total).");
	}
}
