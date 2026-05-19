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
	public async Task TestRunWm2026()
	{
		var wm = InitCompetition(CompetitionType.WM, 2026);
		var simulator = new CompetitionSimulator(wm, Repo);

		Assert.Equal(12, wm.Groups.Count);
		Assert.Equal(48, wm.Participants.Count);
		Assert.Equal(104, wm.GamesByDate.Count);

		foreach (var stage in wm.Stages)
		{
			await simulator.SimulateStage(stage);
		}

		Assert.True(wm.IsFinished);
		var final = wm.LastGame;
		Assert.Equal("Final", final?.Round.Name);
		Assert.NotNull(final?.Winner);
	}

	[Fact]
	public void TestStandings()
	{
		// TODO Test different rules:
		// goal difference, head-to-head, more goals scored etc.
	}

	[Fact]
	public async Task SimulateRound_DoesNotHang_WhenAllGamesArePlaceholders()
	{
		// Reproduces the live freeze: a Round whose only Game has a placeholder team
		// (qualifier returned null — e.g. greedy 3rd-place allocation failed). Pre-fix,
		// SimulateRound's `while (!round.IsFinished)` loop calls SimulateGame, which
		// early-returns because IsReadyToStart is false. CurrentGame stays the same.
		// The loop spins synchronously forever and freezes the browser tab.
		var roundOf16 = new Round { Name = "Round of 16" };
		var koGame = new KoGame(
			idInCompetition: 1,
			qualifierHome: Qualifier.FromGroup("A1"),
			qualifierAway: Qualifier.FromGroup("B2"),
			playedOn: new DateTime(2024, 1, 1));
		koGame.Round = roundOf16;
		koGame.HomeGroupQualifier!.Game = koGame;
		koGame.AwayGroupQualifier!.Game = koGame;
		roundOf16.KoGames.Add(koGame);
		// Qualifier.Get returns null (no Competition/Group wired) → HomeTeam is a placeholder
		// → IsReadyToStart is false.
		koGame.HomeTeam.Type.Should().Be(TeamType.PLACEHOLDER);

		var competition = new Competition { Name = "Dummy", ShortName = "X" };
		var simulator = new CompetitionSimulator(competition, Repo, msGameDelay: 0);

		// Without the progress check this hangs forever. Cap with a generous timeout —
		// the fix should bail in microseconds.
		var act = async () => await simulator.SimulateRound(roundOf16).WaitAsync(TimeSpan.FromSeconds(5));
		await act.Should().NotThrowAsync("simulator must bail when a round can't make progress");
		roundOf16.IsFinished.Should().BeFalse("the stuck game is still SCHEDULED — bail is intentional, not completion");
	}
}
