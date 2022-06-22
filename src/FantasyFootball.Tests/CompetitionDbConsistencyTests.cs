using System;

namespace FantasyFootball.Tests;

public class CompetitionDbConsistencyTests : BaseTest
{
	public CompetitionDbConsistencyTests(ITestOutputHelper output) : base(output, level: LogEventLevel.Debug) { }

	[Fact]
	public async Task TestInitializeEm2020()
	{
		var em2020 = await InitCompetition(CompetitionType.EM, TeamSelectionType.HISTORIC);
		Assert.Equal(2, em2020.Stages.Count);
		Assert.Equal(6, em2020.Groups.Count);
		Assert.Equal(7, em2020.Rounds.Count);
		Assert.Equal(24, em2020.Participants.Count);
		Assert.Equal(51, em2020.GamesByDate.Count);
		Assert.Equal(15, em2020.GamesByDate.Count(g => g is KoGame));

		var groupGames = em2020.Stages.First().Games;
		Assert.All(em2020.Participants, team => { Assert.Equal(3, groupGames.Count(g => g.HomeTeam.Equals(team) || g.AwayTeam.Equals(team))); });

	}

	[Fact]
	public async Task TestInitializeWm2022()
	{
		var wm2022 = await InitCompetition(CompetitionType.WM, TeamSelectionType.HISTORIC);
		Assert.Equal(2, wm2022.Stages.Count);
		Assert.Equal(8, wm2022.Groups.Count);
		Assert.Equal(8, wm2022.Rounds.Count);
		Assert.Equal(32, wm2022.Participants.Count);
		Assert.Equal(64, wm2022.GamesByDate.Count);
		Assert.Equal(16, wm2022.GamesByDate.Count(g => g is KoGame));

	}

	/// <summary> Test expected [ManyToOne] resolution behavior after save/get </summary>
	[Fact]
	public async Task TestManyToOneCompetition()
	{
		var wm2022 = await InitCompetition(CompetitionType.WM, TeamSelectionType.HISTORIC);

		var allGames = wm2022.GamesByDate;
		var groupGames = allGames.Where(g => g is not KoGame);
		var teams = wm2022.Participants;

		var koGames = allGames.Where(g => g is KoGame);

		foreach (var groupGame in groupGames)
		{
			var round = groupGame.Round;
			Assert.NotNull(round);
			Assert.Equal(groupGame.RoundId, round.Id);
			Assert.Contains(groupGame, round.RegularGames);

			var stage = round.Stage;
			Assert.NotNull(stage);
			Assert.Equal(round.StageId, stage.Id);
			Assert.Contains(groupGame, round.RegularGames);

			var competition = stage.Competition;
			Assert.NotNull(competition);
			Assert.Equal(stage.CompetitionId, competition.Id);
			Assert.Contains(groupGame, round.RegularGames);
		}
	}

	[Fact]
	public void TestKoGameSerialization()
	{
		var koGame = new KoGame(2, Qualifier.FromGroup("A1"), Qualifier.FromGame(13), DateTime.Now);

		Repo.Save(koGame);
		var koGameDb = Repo.Get<KoGame>(1)!;
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
	}
}
