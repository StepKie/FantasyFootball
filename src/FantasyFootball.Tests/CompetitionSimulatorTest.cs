namespace FantasyFootball.Tests;

public class CompetitionSimulatorTest : BaseTest
{
	public CompetitionSimulatorTest(ITestOutputHelper output) : base(output, level: LogEventLevel.Debug) { }

	[Time]
	[Fact]
	public async Task TestRunEm2020()
	{
		var em2020 = await InitCompetition(CompetitionType.EM, TeamSelectionType.HISTORIC);

		var simulator = new CompetitionSimulator(em2020, Repo);
		await simulator.Simulate();

		foreach (var game in em2020.GamesByDate)
		{
			Repo.Save(game);
		}

		var final = em2020.LastGame;
		var winner = final?.Winner;
		Assert.True(final?.Round.Name == "Final");
		Assert.NotNull(winner);
		Repo.Save(em2020);

		var fromDb = Repo.Get<Competition>(em2020.Id);
		var finalDb = fromDb?.GamesByDate.Last();
		Assert.Equal(winner, finalDb?.Winner);
	}

	[Time]
	[Fact]
	public async Task TestRunWm2022()
	{
		var wm2022 = await InitCompetition(CompetitionType.WM, TeamSelectionType.HISTORIC);

		var simulator = new CompetitionSimulator(wm2022, Repo);
		var groups = wm2022.Groups;
		var groupStage = wm2022.Stages[0];
		var koStage = wm2022.Stages[1];
		var groupRounds = wm2022.Rounds.GetRange(0, 3);
		var roundOf16 = koStage.Rounds.First();

		Assert.All(roundOf16.KoGames, g =>
		{
			Assert.True(g.HomeQualifier is GroupQualifier);
			Assert.Null(g.HomeQualifier.Get());
			Assert.True(g.HomeTeam.ShortName == "TBD");
		});

		await simulator.SimulateStage(groupStage);

		var groupTables = groups.Select(g => g.GetStandings());
		var firstPlaceTeams = groupTables.Select(table => table[0].Team).ToList();
		var secondPlaceTeams = groupTables.Select(table => table[1].Team).ToList();
		Assert.All(roundOf16.KoGames, g =>
		{
			Assert.NotNull(g.HomeQualifier.Get());
			Assert.Contains(g.HomeTeam, firstPlaceTeams);
			Assert.Contains(g.AwayTeam, secondPlaceTeams);
		});

		Assert.Equal(roundOf16.KoGames.First().HomeTeam, groups[0].GetStandings()[0].Team);
		Assert.Equal(roundOf16.KoGames.First().AwayTeam, groups[1].GetStandings()[1].Team);

		//await simulator.SimulateStage(koStage);

		foreach (var koRound in koStage.Rounds)
		{
			await simulator.SimulateRound(koRound);
			Log.Debug("Test");
		}

		Assert.True(wm2022.IsFinished);
		var final = wm2022.LastGame;
		var winner = final?.Winner;
		Assert.True(final?.Round.Name == "Final");
		Assert.NotNull(winner);
		Repo.Save(wm2022);

		var fromDb = Repo.Get<Competition>(wm2022.Id);
		var finalDb = fromDb?.GamesByDate.Last();
		Assert.Equal(winner, finalDb?.Winner);
	}

	[Fact]
	public void TestStandings()
	{
		// TODO Test different rules:
		// goal difference, head-to-head, more goals scored etc.
	}
}
