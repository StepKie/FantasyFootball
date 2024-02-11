using System;

namespace FantasyFootball.Tests;

public class CompetitionDbConsistencyTests(ITestOutputHelper output) : BaseTest(output, level: LogEventLevel.Debug)
{
	[InlineData(CompetitionType.EM, 2024, 2, 6, 7, 24, 51, 15)]
	[InlineData(CompetitionType.WM, 2022, 2, 8, 8, 32, 64, 16)]
	[Theory]
	public void TestInitializeCompetition(CompetitionType type, int year, int expectedStages, int expectedGroups, int expectedRounds, int expectedParticipants, int expectedGames, int expectedKoGames)
	{
		var comp = InitCompetition(type, year);
		Assert.Equal(expectedStages, comp.Stages.Count);
		Assert.Equal(expectedGroups, comp.Groups.Count);
		Assert.Equal(expectedRounds, comp.Rounds.Count);
		Assert.Equal(expectedParticipants, comp.Participants.Count);
		Assert.Equal(expectedGames, comp.GamesByDate.Count);
		Assert.Equal(expectedKoGames, comp.GamesByDate.Count(g => g is KoGame));

		var groupGames = comp.Stages.First().Games;
		Assert.All(comp.Participants, team => Assert.Equal(3, groupGames.Count(g => g.HomeTeam.Equals(team) || g.AwayTeam.Equals(team))));
	}

	/// <summary> Test expected [ManyToOne] resolution behavior after save/get </summary>
	[Fact]
	public void TestManyToOneCompetition()
	{
		var wm2022 = InitCompetition(CompetitionType.WM, 2022);

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

		Repo.Save(koGame);
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
			Rounds =
			[
				new Round { Name = "Round 1", }
			]
		};

		Repo.Save(stage);
		var stageDb = Repo.Get<Stage>(1)!;
		var roundDb = Repo.Get<Round>(1)!;
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
			Stages =
			[
				new Stage
				{
					Name = "Stage 1",
					Rounds =
					[
						new Round()
						{
							Name = "Round 1",
							RegularGames =
							[
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
							]
						}
					]
				}
			]
		};

		Repo.Save(competition);
		var stageDb = Repo.Get<Stage>(1)!;
		var competitionDb = Repo.Get<Competition>(1)!;
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
		Repo.Save(game);

		var gameDb = Repo.Get<Game>(1)!;
		// Null due to CascadeOperation.Read test is expected to fail when we change the annotation
		Assert.Null(gameDb.HomeTeam);
		Assert.NotNull(gameDb.AwayTeam);
	}
}
