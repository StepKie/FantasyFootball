using FantasyFootball.Data;
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
		var em2020 = await new EmCompetitionFactory(Repo).Create();
		Repo.Save(em2020);

		// To check whether  the save operation inserts correctly with cascades and child relationships
		var competitionsDb = Repo.GetAll<Competition>();
		var stagesDb = Repo.GetAll<Stage>();
		var roundsDb = Repo.GetAll<Round>();
		var groupsDb = Repo.GetAll<Group>();
		var teamsDb = Repo.GetAll<Team>();
		var gamesDb = Repo.GetAll<Game>();
		var emFromDb = Repo.Get<Competition>(em2020.Id);
		var teamsAfterEm2020 = Repo.GetAll<Team>();

		Assert.Equal(6, em2020.Stages[0].Groups.Count);
		Assert.Equal(teams.Count, teamsAfterEm2020.Count);

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
		var finalDb = fromDb.GamesByDate.Last();
		Assert.Equal(winner, finalDb.Winner);
	}

	[Fact]
	public async Task TestInitializeRandomEm()
	{
		var groups = Repo.GetAll<Group>();
		var teams = Repo.GetAll<Team>();
		var games = Repo.GetAll<Game>();
		var countries = Repo.GetAll<Country>();
		var em2020 = await new RandomEmCompetitionFactory(Repo).Create();
		Repo.Save(em2020);
		var teamsAfterEm2020 = Repo.GetAll<Team>();

		Assert.Equal(6, em2020.Stages[0].Groups.Count);
		Assert.Equal(teams.Count, teamsAfterEm2020.Count);
	}
}
