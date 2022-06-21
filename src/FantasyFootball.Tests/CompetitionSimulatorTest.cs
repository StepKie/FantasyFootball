using FantasyFootball.Data;
using FantasyFootball.Data.CompetitionFactories;
using FantasyFootball.Models;

namespace FantasyFootball.Tests;

public class CompetitionSimulatorTest : BaseTest
{
	public CompetitionSimulatorTest(ITestOutputHelper output) : base(output, level: LogEventLevel.Debug) { }

	[Time]
	[Fact]
	public async Task TestRunEm2020()
	{
		var competitions = Repo.GetAll<Competition>();
		var stages = Repo.GetAll<Stage>();
		var rounds = Repo.GetAll<Round>();
		var groups = Repo.GetAll<Group>();
		var teams = Repo.GetAll<Team>();
		var games = Repo.GetAll<Game>();
		var em2020 = await new Em2020CompetitionFactory(Repo).Create();
		Repo.Save(em2020);

		// To check whether  the save operation inserts correctly with cascades and child relationships
		var competitionsDb = Repo.GetAll<Competition>();
		var stagesDb = Repo.GetAll<Stage>();
		var roundsDb = Repo.GetAll<Round>();
		var groupsDb = Repo.GetAll<Group>();
		var teamsDb = Repo.GetAll<Team>();
		var gamesDb = Repo.GetAll<Game>();
		var emFromDb = Repo.Get<Competition>(em2020.Id);

		Assert.Equal(6, em2020.Stages[0].Groups.Count);

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
		var wm2022 = await new Wm2022CompetitionFactory(Repo).Create();
		Repo.Save(wm2022);
		var roundOf16 = wm2022.Rounds[3].KoGames;
		// To check whether  the save operation inserts correctly with cascades and child relationships
		var competitionsDb = Repo.GetAll<Competition>();
		var qualifiers = Repo.GetAll<GroupQualifier>();
		var qualifiersG = Repo.GetAll<Qualifier>();
		var wmFromDb = Repo.Get<Competition>(wm2022.Id);
		var roundOf16Db = wmFromDb.Rounds[3].KoGames;
		Assert.Equal(8, wm2022.Stages[0].Groups.Count);

		var simulator = new CompetitionSimulator(wm2022, Repo);
		await simulator.Simulate();

		foreach (var game in wm2022.GamesByDate)
		{
			Repo.Save(game);
		}

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
	public async Task TestInitializeRandomEm()
	{
		var groups = Repo.GetAll<Group>();
		var teams = Repo.GetAll<Team>();
		var games = Repo.GetAll<Game>();
		var countries = Repo.GetAll<Country>();
		var em2020 = await new DefaultEmCompetitionFactory(Repo).Create();
		Repo.Save(em2020);
		var teamsAfterEm2020 = Repo.GetAll<Team>();

		Assert.Equal(6, em2020.Stages[0].Groups.Count);
		Assert.Equal(teams.Count, teamsAfterEm2020.Count);
	}

	[Fact]
	public void TestKoGameSerialization()
	{
		var koGame = new KoGame
		{
			Qualifiers = new() 
			{
				GroupQualifier.FromGroup("A1"),
				//new() { Name = "Moo" },
				new() { Name = "Meh" }
			},
		};
		Repo.Save(koGame);
		var qualifiers = Repo.GetAll<Qualifier>();
		var groupqualifiers = Repo.GetAll<GroupQualifier>();
		var koGameDb = Repo.Get<KoGame>(1)!;
		var qual = koGameDb.HomeQualifier;
		var qual2 = koGameDb.AwayQualifier;
		Log.Debug("Finito");
	}

	[Fact]
	public void TestGameSerialization()
	{
		var game = new Game
		{
			HomeTeam = new Team { Name = "Team 1" },
			AwayTeam = new Team { Name = "Team 2" },
		};
		Repo.Save(game);

		var gameDb = Repo.Get<Game>(1)!;
		var team1 = gameDb.HomeTeam;
		var team2 = gameDb.AwayTeam;
		Log.Debug("Finito");
	}
}
