using System;

namespace FantasyFootball.Tests;

public class CompetitionDbConsistencyTests : BaseTest
{
	public CompetitionDbConsistencyTests(ITestOutputHelper output) : base(output, level: LogEventLevel.Debug) { }

	/// <summary> TODO Use Theory to remove duplication with other competition types </summary>
	[Fact]
	public async Task TestInitializeEm2020()
	{
		var em2020 = await InitCompetition(CompetitionType.EM);
		Assert.Equal(2, em2020.Stages.Count);
		Assert.Equal(6, em2020.Groups.Count);
		Assert.Equal(7, em2020.Rounds.Count);
		Assert.Equal(24, em2020.Participants.Count);
		Assert.Equal(51, em2020.GamesByDate.Count);
		Assert.Equal(15, em2020.GamesByDate.Count(g => g is KoGame));

		var groupGames = em2020.Stages.First().Games;
		Assert.All(em2020.Participants, team => Assert.Equal(3, groupGames.Count(g => g.HomeTeam.Equals(team) || g.AwayTeam.Equals(team))));

	}

	[Fact]
	public async Task TestInitializeWm2022()
	{
		var wm2022 = await InitCompetition(CompetitionType.WM);
		Assert.Equal(2, wm2022.Stages.Count);
		Assert.Equal(8, wm2022.Groups.Count);
		Assert.Equal(8, wm2022.Rounds.Count);
		Assert.Equal(32, wm2022.Participants.Count);
		Assert.Equal(64, wm2022.GamesByDate.Count);
		Assert.Equal(16, wm2022.GamesByDate.Count(g => g is KoGame));

		var groupGames = wm2022.Stages.First().Games;
		Assert.All(wm2022.Participants, team => Assert.Equal(3, groupGames.Count(g => g.HomeTeam.Equals(team) || g.AwayTeam.Equals(team))));

	}

	/// <summary> Test expected [ManyToOne] resolution behavior after save/get </summary>
	[Fact]
	public async Task TestManyToOneCompetition()
	{
		var wm2022 = await InitCompetition(CompetitionType.WM);

		var allGames = wm2022.GamesByDate;
		var groupGames = allGames.Where(g => g is not KoGame);
		var teams = wm2022.Participants;

		var koGames = allGames.Where(g => g is KoGame);

		foreach (var group in wm2022.Groups)
		{
			var groupStage = group.Stage;
			Assert.NotNull(groupStage);
			Assert.Equal(group.StageId, groupStage.Id);

			foreach (var groupGame in group.Games)
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
	}

	[Fact]
	public void TestKoGameSerialization()
	{
		var koGame = new KoGame(2, Qualifier.FromGroup("A1"), Qualifier.FromGame(13), DateTime.Now);

		Repo.Insert(koGame);
		var koGameDb = Repo.Get<KoGame>(1)!;
		Assert.NotNull(koGameDb);
		// TODO More
	}

	[Fact]
	public void TestManyToOneStageRounds()
	{
		Stage stage = new()
		{
			Name = "Stage 1",
			Rounds = new()
			{
				new Round { Name = "Round 1", }
			}
		};

		Repo.Insert(stage);
		Stage stageDb = Repo.Get<Stage>(1)!;
		Round roundDb = Repo.Get<Round>(1)!;
		Assert.Equal(stageDb.Id, roundDb.StageId);
		Assert.Equal(stageDb, roundDb.Stage);
		Assert.Equal("Stage 1", stageDb.Name);
	}

	[Fact]
	public void TestManyToOneCompetitionStages()
	{
		Competition competition = new()
		{
			Name = "Competition 1",
			Stages = new()
			{
				new Stage
				{
					Name = "Stage 1",
					Rounds = new()
					{
						new Round()
						{
							Name = "Round 1",
							RegularGames = new()
							{
								new Game
								{
									Name = "Game 1",
									PlayedOn = DateTime.Now,
									HomeTeam = new Team { Name = "Team 1", ShortName = "T1" },
									AwayTeam = new Team { Name = "Team 2", ShortName = "T2" },
									HomeScore = 2,
									AwayScore = 1,
									State = GameState.FINISHED,
									Ending = GameEnd.EXTRA_TIME,
								}
							}
						}
					}
				}
			}
		};

		Repo.Insert(competition);
		Stage stageDb = Repo.Get<Stage>(1)!;
		Competition competitionDb = Repo.Get<Competition>(1)!;
		Assert.Equal(competitionDb.Id, stageDb.CompetitionId);
		Assert.Equal(competitionDb, stageDb.Competition);
		Assert.Equal("Competition 1", competitionDb.Name);
	}

	[Fact]
	public void TestGameSerialization()
	{
		var game = new Game
		{
			HomeTeam = new Team { Name = "Team 1" },
			AwayTeam = Repo.Get<Team>(1)!,
		};
		Repo.Insert(game);

		var gameDb = Repo.Get<Game>(1)!;
		// Null due to CascadeOperation.Read test is expected to fail when we change the annotation
		Assert.Null(gameDb.HomeTeam);
		Assert.NotNull(gameDb.AwayTeam);
	}
}
