using FantasyFootball.Repositories;

namespace FantasyFootball.Tests;

/// <summary>
/// Statistical sanity that the new flat simulator and the old graph
/// simulator are doing approximately the same thing. Bit-identical
/// parity is impossible because the old code uses <c>new Random()</c>
/// per goal (every call seeds from the system clock, so same-tick
/// calls produce identical values) — that's a real bug the new code
/// fixes by holding a single seeded RNG. So we compare distributions,
/// not individual outcomes.
///
/// Lives alongside the old simulator and disappears with it in the
/// cleanup PR.
/// </summary>
public class HeadToHeadParityTests : BaseTest
{
	public HeadToHeadParityTests(ITestOutputHelper output) : base(output) { }

	const int SampleSize = 30;

	[Fact]
	public async Task GoalsPerGame_NewAndOld_AreInSameBallpark()
	{
		// Run N sims with each simulator on wm-2026. Compare average goals
		// per game across all runs; should land in the same neighborhood
		// (both are sampling from Poisson(~2.5)).
		var oldAvg = await MeasureOldGoalsPerGame(SampleSize);
		var newAvg = MeasureNewGoalsPerGame(SampleSize);
		Output.WriteLine($"Old goals/game: {oldAvg:F2}");
		Output.WriteLine($"New goals/game: {newAvg:F2}");

		Math.Abs(oldAvg - newAvg).Should().BeLessThan(0.5,
			"both models sample Poisson(~2.5) for total goals — their averages should agree to within half a goal per match");
	}

	[Fact]
	public async Task HomeWinRate_StrongFavorite_BothModelsTrendToFavorite()
	{
		// Pick a fixed favored-vs-underdog pairing across many sims. Both
		// models should give the favorite a majority of wins.
		var oldFavRate = await MeasureOldFavoriteWinRate(SampleSize);
		var newFavRate = MeasureNewFavoriteWinRate(SampleSize);
		Output.WriteLine($"Old favorite win rate: {oldFavRate:P0}");
		Output.WriteLine($"New favorite win rate: {newFavRate:P0}");

		oldFavRate.Should().BeGreaterThan(0.5, "old simulator should favor higher-ELO team");
		newFavRate.Should().BeGreaterThan(0.5, "new simulator should favor higher-ELO team");
	}

	async Task<double> MeasureOldGoalsPerGame(int sampleSize)
	{
		long totalGoals = 0, totalGames = 0;
		for (int i = 0; i < sampleSize; i++)
		{
			var comp = CompetitionFactory.Default(CompetitionType.WM, DataService, 2026).Create();
			var sim = new CompetitionSimulator(comp, Repo) { Quiet = true };
			await sim.Simulate();
			foreach (var g in comp.GamesByDate)
			{
				totalGoals += g.HomeScore + g.AwayScore;
				totalGames++;
			}
		}
		return totalGames == 0 ? 0 : (double)totalGoals / totalGames;
	}

	double MeasureNewGoalsPerGame(int sampleSize)
	{
		var store = new EmbeddedCompetitionDefinitionStore();
		var factory = new FlatCompetitionFactory(store);
		var registry = new DataServiceTeamRegistry(DataService);
		var scoreModel = new EloScoreModel(registry, new Random(42));
		var simulator = new FlatCompetitionSimulator(scoreModel);

		long totalGoals = 0, totalGames = 0;
		for (int i = 0; i < sampleSize; i++)
		{
			var comp = factory.Create(new HistoricalSpec { DefinitionId = "wm-2026" });
			simulator.Simulate(comp);
			foreach (var g in comp.Games)
			{
				var r = g.Result!.Value;
				totalGoals += r.HomeScore + r.AwayScore;
				totalGames++;
			}
		}
		return totalGames == 0 ? 0 : (double)totalGoals / totalGames;
	}

	async Task<double> MeasureOldFavoriteWinRate(int sampleSize)
	{
		// Use the first group game of a fresh wm-2026 competition and assume
		// the higher-ELO team is the favorite. Run N times.
		int favWins = 0, played = 0;
		for (int i = 0; i < sampleSize; i++)
		{
			var comp = CompetitionFactory.Default(CompetitionType.WM, DataService, 2026).Create();
			var sim = new CompetitionSimulator(comp, Repo) { Quiet = true };
			await sim.Simulate();

			foreach (var g in comp.GamesByDate.Take(20))     // first 20 games to widen the sample per run
			{
				var home = g.HomeTeam!;
				var away = g.AwayTeam!;
				if (home.Elo == away.Elo) { continue; }
				var fav = home.Elo > away.Elo ? home : away;
				var homeIsFav = fav == home;
				if ((homeIsFav && g.HomeScore > g.AwayScore) || (!homeIsFav && g.AwayScore > g.HomeScore))
				{
					favWins++;
				}
				played++;
			}
		}
		return played == 0 ? 0 : (double)favWins / played;
	}

	double MeasureNewFavoriteWinRate(int sampleSize)
	{
		var store = new EmbeddedCompetitionDefinitionStore();
		var factory = new FlatCompetitionFactory(store);
		var registry = new DataServiceTeamRegistry(DataService);
		var scoreModel = new EloScoreModel(registry, new Random(42));
		var simulator = new FlatCompetitionSimulator(scoreModel);

		int favWins = 0, played = 0;
		for (int i = 0; i < sampleSize; i++)
		{
			var comp = factory.Create(new HistoricalSpec { DefinitionId = "wm-2026" });
			simulator.Simulate(comp);

			foreach (var g in comp.Games.OfType<FlatGroupGame>().Take(20))
			{
				var homeElo = registry.EloOf(g.HomeTeamId);
				var awayElo = registry.EloOf(g.AwayTeamId);
				if (homeElo == awayElo) { continue; }
				var homeIsFav = homeElo > awayElo;
				var r = g.Result!.Value;
				if ((homeIsFav && r.HomeScore > r.AwayScore) || (!homeIsFav && r.AwayScore > r.HomeScore))
				{
					favWins++;
				}
				played++;
			}
		}
		return played == 0 ? 0 : (double)favWins / played;
	}
}
