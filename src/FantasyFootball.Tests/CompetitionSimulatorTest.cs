namespace FantasyFootball.Tests;

public class CompetitionSimulatorTest(ITestOutputHelper output) : BaseTest(output, level: LogEventLevel.Debug)
{
	[Fact]
	public async Task TestRunEm2020()
	{
		var em2020 = InitCompetition(CompetitionType.EM);

		var simulator = new CompetitionSimulator(em2020, Repo);
		var groupStage = em2020.Stages[0];
		var koStage = em2020.Stages[1];
		await simulator.SimulateStage(groupStage);
		await simulator.SimulateStage(koStage);
		var final = em2020.LastGame;
		var winner = final?.Winner;
		winner.Should().NotBeNull();
		final!.Round.Name.Should().BeEquivalentTo("Final");
		Repo.Save(em2020);

		var fromDb = Repo.Get<Competition>(em2020.Id);
		var finalDb = fromDb?.GamesByDate.Last();
		Assert.Equal(winner, finalDb?.Winner);
	}

	[Fact]
	public async Task TestRunWm2022()
	{
		var wm2022 = InitCompetition(CompetitionType.WM);

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
			Assert.Equal("TBD", g.HomeTeam.ShortName);
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

		foreach (var koRound in koStage.Rounds)
		{
			await simulator.SimulateRound(koRound);
		}

		Assert.True(wm2022.IsFinished);
		var final = wm2022.LastGame;
		var winner = final?.Winner;
		Assert.Equal("Final", final?.Round.Name);
		Assert.NotNull(winner);
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
