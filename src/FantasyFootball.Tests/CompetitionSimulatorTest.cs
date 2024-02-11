namespace FantasyFootball.Tests;

public class CompetitionSimulatorTest(ITestOutputHelper output) : BaseTest(output, level: LogEventLevel.Debug)
{
	[InlineData(2024)]
	[InlineData(2020)]
	[InlineData(2016)]
	[Theory]
	public async Task TestRunEm(int year)
	{
		var em = InitCompetition(CompetitionType.EM, year);

		var simulator = new CompetitionSimulator(em, Repo);
		var groupStage = em.Stages[0];
		var koStage = em.Stages[1];
		await simulator.SimulateStage(groupStage);
		await simulator.SimulateStage(koStage);
		var final = em.LastGame;
		var winner = final?.Winner;
		winner.Should().NotBeNull();
		final!.Round.Name.Should().BeEquivalentTo("Final");
		Repo.Save(em);

		var fromDb = Repo.Get<Competition>(em.Id);
		var finalDb = fromDb?.GamesByDate.Last();
		Assert.Equal(winner, finalDb?.Winner);
	}

	[InlineData(2022)]
	[InlineData(2018)]
	[Theory]
	public async Task TestRunWm(int year)
	{
		var wm = InitCompetition(CompetitionType.WM, year);

		var simulator = new CompetitionSimulator(wm, Repo);
		var groups = wm.Groups;
		var groupStage = wm.Stages[0];
		var koStage = wm.Stages[1];
		var groupRounds = wm.Rounds[0..3];
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

		Assert.True(wm.IsFinished);
		var final = wm.LastGame;
		var winner = final?.Winner;
		Assert.Equal("Final", final?.Round.Name);
		Assert.NotNull(winner);
		var fromDb = Repo.Get<Competition>(wm.Id);
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
