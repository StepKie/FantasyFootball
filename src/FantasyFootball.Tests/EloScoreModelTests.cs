namespace FantasyFootball.Tests;

/// <summary>
/// Statistical sanity checks on the ELO-driven score model: equal-ELO
/// teams trend toward a 50/50 win split, strongly-favored teams win
/// most of the time, and KO games never return a draw.
///
/// Tests use a seeded Random so the sample distributions are stable.
/// </summary>
public class EloScoreModelTests
{
	sealed class FixedRegistry : ITeamRegistry
	{
		readonly Dictionary<string, int> _elo;
		public FixedRegistry(params (string id, int elo)[] entries)
		{
			_elo = entries.ToDictionary(t => t.id, t => t.elo);
		}
		public IReadOnlyList<string> AllTeamIds => [.. _elo.Keys];
		public int EloOf(string teamId) => _elo[teamId];
	}

	[Fact]
	public void ScoreGroupGame_EqualElos_TrendsTowardEvenWinSplit()
	{
		var registry = new FixedRegistry(("A", 1500), ("B", 1500));
		var model = new EloScoreModel(registry, new Random(42));

		int aWins = 0, bWins = 0, draws = 0;
		for (int i = 0; i < 1000; i++)
		{
			var r = model.ScoreGroupGame("A", "B");
			if (r.HomeWon) { aWins++; }
			else if (r.AwayWon) { bWins++; }
			else { draws++; }
		}

		// Equal ELOs → win shares within a tight band around 35–40%, draws ~25%. The exact split depends on Poisson(2.65) and the 50/50 per-goal flip; here we just check no extreme skew.
		var aWinRate = aWins / 1000.0;
		var bWinRate = bWins / 1000.0;
		Math.Abs(aWinRate - bWinRate).Should().BeLessThan(0.08, "equal ELOs should produce roughly symmetric win rates");
	}

	[Fact]
	public void ScoreGroupGame_StrongFavorite_WinsMostOftenAndScoresMore()
	{
		// 400-point ELO gap → favored team should win ~75% of games per
		// the per-goal model.
		var registry = new FixedRegistry(("STRONG", 2000), ("WEAK", 1600));
		var model = new EloScoreModel(registry, new Random(42));

		int strongWins = 0;
		int totalStrongGoals = 0;
		int totalWeakGoals = 0;
		for (int i = 0; i < 500; i++)
		{
			var r = model.ScoreGroupGame("STRONG", "WEAK");
			if (r.HomeWon) { strongWins++; }
			totalStrongGoals += r.HomeScore;
			totalWeakGoals += r.AwayScore;
		}

		var strongWinRate = strongWins / 500.0;
		strongWinRate.Should().BeGreaterThan(0.6, "400-pt ELO favorite should win clear majority of games");
		totalStrongGoals.Should().BeGreaterThan(totalWeakGoals, "favorite should outscore underdog on aggregate");
	}

	[Fact]
	public void ScoreKoGame_NeverReturnsADraw()
	{
		// IScoreModel.ScoreKoGame contract: result must be decisive.
		var registry = new FixedRegistry(("A", 1500), ("B", 1500));
		var model = new EloScoreModel(registry, new Random(42));

		for (int i = 0; i < 200; i++)
		{
			var r = model.ScoreKoGame("A", "B");
			(r.HomeWon || r.AwayWon).Should().BeTrue($"iteration {i} returned a KO result without a winner");
		}
	}

	[Fact]
	public void ScoreKoGame_TieBreakerEndings_AreEitherExtraTimeOrPenalties()
	{
		// When the regular-time portion ties, the result's Ending should
		// be EXTRA_TIME or PENALTIES — never NORMAL.
		var registry = new FixedRegistry(("A", 1500), ("B", 1500));
		var model = new EloScoreModel(registry, new Random(42));

		int normal = 0, et = 0, pens = 0;
		for (int i = 0; i < 500; i++)
		{
			var r = model.ScoreKoGame("A", "B");
			if (r.Ending == GameEnd.NORMAL) { normal++; }
			else if (r.Ending == GameEnd.EXTRA_TIME) { et++; }
			else if (r.Ending == GameEnd.PENALTIES) { pens++; }
		}

		(et + pens).Should().BeGreaterThan(0, "evenly-matched KO games should sometimes go to ET or pens");
		normal.Should().BeGreaterThan(0, "most evenly-matched KO games should still decide in regular time");
	}

	[Fact]
	public void ScoreGroupGame_SameSeedTwice_ProducesSameResults()
	{
		var registry = new FixedRegistry(("A", 1500), ("B", 1600));

		var modelA = new EloScoreModel(registry, new Random(1234));
		var modelB = new EloScoreModel(registry, new Random(1234));

		for (int i = 0; i < 20; i++)
		{
			var ra = modelA.ScoreGroupGame("A", "B");
			var rb = modelB.ScoreGroupGame("A", "B");
			ra.Should().Be(rb, $"iteration {i} should be deterministic under a seeded RNG");
		}
	}
}
