namespace FantasyFootball.Tests;

/// <summary>
/// Unit tests for the projections over the flat Competition shape.
/// Hand-built Competition records — no factory, no JSON loader, no
/// simulator — just verifying the extension methods slice/dice the
/// model the way the docs claim.
/// </summary>
public class CompetitionExtensionsTests
{
	static Competition BuildMiniWc(bool simulateFinal = false, bool homeWinsFinal = true)
	{
		// 2 groups × 2 teams, single round-robin in each, then a Final.
		// Tiny but exercises GroupGame + KoGame + Result presence/absence.
		var stages = new[]
		{
			new Stage("group", "Group", 0),
			new Stage("ko",    "Knockout", 1),
		};
		var rounds = new[]
		{
			new Round("g-r1",  "Group Round 1", "group", 0),
			new Round("final", "Final",         "ko",    1),
		};
		var games = new Game[]
		{
			new GroupGame
			{
				Id = 1, PlayedOn = new(2026, 6, 1, 16, 0, 0), RoundId = "g-r1",
				GroupLetter = "A", HomeTeamId = "MEX", AwayTeamId = "RSA",
				Result = new() { HomeScore = 2, AwayScore = 1, Ending = GameEnd.NORMAL },
			},
			new GroupGame
			{
				Id = 2, PlayedOn = new(2026, 6, 1, 20, 0, 0), RoundId = "g-r1",
				GroupLetter = "B", HomeTeamId = "BRA", AwayTeamId = "ARG",
				Result = new() { HomeScore = 0, AwayScore = 3, Ending = GameEnd.NORMAL },
			},
			new KoGame
			{
				Id = 3, PlayedOn = new(2026, 6, 28, 20, 0, 0), RoundId = "final",
				HomeQual = "A1", AwayQual = "B1",
				HomeTeamId = "MEX", AwayTeamId = "ARG",
				Result = simulateFinal
					? new Result { HomeScore = homeWinsFinal ? 1 : 0, AwayScore = homeWinsFinal ? 0 : 2, Ending = GameEnd.NORMAL }
					: null,
			},
		};
		return new Competition
		{
			Id = 1, Title = "Mini WC", Type = CompetitionType.WM, Year = 2026,
			DefinitionId = "mini-wc-2026", FormatId = "mini",
			GroupAssignments = [["MEX", "RSA"], ["BRA", "ARG"]],
			Stages = stages, Rounds = rounds, Games = games,
		};
	}

	[Fact]
	public void AllTeamIds_UnionsGroupAssignments()
	{
		var c = BuildMiniWc();
		c.AllTeamIds().Should().BeEquivalentTo(["MEX", "RSA", "BRA", "ARG"]);
	}

	[Fact]
	public void GroupGames_FiltersByLetter()
	{
		var c = BuildMiniWc();
		c.GroupGames("A").Should().ContainSingle().Which.Id.Should().Be(1);
		c.GroupGames("B").Should().ContainSingle().Which.Id.Should().Be(2);
		c.GroupGames("C").Should().BeEmpty();
	}

	[Fact]
	public void RoundGames_FiltersByRoundId()
	{
		var c = BuildMiniWc();
		c.RoundGames("g-r1").Select(g => g.Id).Should().BeEquivalentTo([1, 2]);
		c.RoundGames("final").Select(g => g.Id).Should().BeEquivalentTo([3]);
		c.RoundGames("unknown").Should().BeEmpty();
	}

	[Fact]
	public void StageGames_ResolvesViaRoundStageId()
	{
		var c = BuildMiniWc();
		c.StageGames("group").Select(g => g.Id).Should().BeEquivalentTo([1, 2]);
		c.StageGames("ko").Select(g => g.Id).Should().BeEquivalentTo([3]);
	}

	[Fact]
	public void SameRoundGames_ExcludesSelf()
	{
		var c = BuildMiniWc();
		var game1 = c.Games[0];
		c.SameRoundGames(game1).Select(g => g.Id).Should().BeEquivalentTo([2]);
	}

	[Fact]
	public void SameGroupGames_ExcludesSelf_AndIsEmptyForKoGames()
	{
		var c = BuildMiniWc();
		var groupGame = c.Games[0];        // GroupGame A
		var koGame = c.Games[2];           // KoGame final
		c.SameGroupGames(groupGame).Should().BeEmpty("only one game per group in this fixture");
		c.SameGroupGames(koGame).Should().BeEmpty("KO games are not group games");
	}

	[Fact]
	public void LastFinishedGame_ReturnsLatestPlayedByDate()
	{
		var c = BuildMiniWc(simulateFinal: true);
		c.LastFinishedGame()!.Id.Should().Be(3, "final has the latest PlayedOn and a Result");
	}

	[Fact]
	public void LastFinishedGame_IgnoresUnplayedGames()
	{
		var c = BuildMiniWc(simulateFinal: false);
		c.LastFinishedGame()!.Id.Should().Be(2, "game 2 (later group game) is the last with a Result");
	}

	[Fact]
	public void CurrentGame_ReturnsEarliestScheduled()
	{
		var c = BuildMiniWc(simulateFinal: false);
		c.CurrentGame()!.Id.Should().Be(3, "only the final lacks a Result");
	}

	[Fact]
	public void CurrentGame_ReturnsNullWhenAllPlayed()
	{
		var c = BuildMiniWc(simulateFinal: true);
		c.CurrentGame().Should().BeNull();
	}

	[Fact]
	public void IsFinished_TrueOnlyWhenAllResultsPresent()
	{
		BuildMiniWc(simulateFinal: false).IsFinished().Should().BeFalse();
		BuildMiniWc(simulateFinal: true).IsFinished().Should().BeTrue();
	}

	[Fact]
	public void WinnerTeamId_PicksHomeIfHomeWins()
	{
		var c = BuildMiniWc(simulateFinal: true, homeWinsFinal: true);
		c.WinnerTeamId().Should().Be("MEX");
	}

	[Fact]
	public void WinnerTeamId_PicksAwayIfAwayWins()
	{
		var c = BuildMiniWc(simulateFinal: true, homeWinsFinal: false);
		c.WinnerTeamId().Should().Be("ARG");
	}

	[Fact]
	public void WinnerTeamId_NullIfFinalNotPlayed()
	{
		var c = BuildMiniWc(simulateFinal: false);
		c.WinnerTeamId().Should().BeNull();
	}

	[Fact]
	public void HomeTeamId_ReadsFromBaseAcrossGameKinds()
	{
		var c = BuildMiniWc(simulateFinal: true);
		c.Games[0].HomeTeamId.Should().Be("MEX");
		c.Games[2].HomeTeamId.Should().Be("MEX");
	}

	[Fact]
	public void HomeTeamId_NullForUnresolvedKoGame()
	{
		var unresolved = new KoGame
		{
			Id = 99, PlayedOn = DateTime.Now, RoundId = "final",
			HomeQual = "A1", AwayQual = "B1",
		};
		unresolved.IsFullyInitialized.Should().BeFalse();
	}

	[Fact]
	public void Format_GroupGame_ScheduledShowsVS()
	{
		var c = BuildMiniWc(simulateFinal: false);
		var unplayed = new GroupGame
		{
			Id = 10, PlayedOn = new(2026, 6, 1, 16, 0, 0), RoundId = "g-r1",
			GroupLetter = "A", HomeTeamId = "MEX", AwayTeamId = "RSA",
		};
		unplayed.Format().Should().Be("[10] 01.06.2026 16:00 MEX v RSA  [Group A / g-r1]");
	}

	[Fact]
	public void Format_GroupGame_PlayedShowsScore()
	{
		var c = BuildMiniWc();
		c.Games[0].Format().Should().Be("[1] 01.06.2026 16:00 MEX 2-1 RSA  [Group A / g-r1]");
	}

	[Fact]
	public void Format_KoGame_UnresolvedShowsQualifiers()
	{
		var unresolved = new KoGame
		{
			Id = 50, PlayedOn = new(2026, 6, 28, 20, 0, 0), RoundId = "r32",
			HomeQual = "A1", AwayQual = "B2",
		};
		unresolved.Format().Should().Be("[50] 28.06.2026 20:00 A1 v B2  [r32]");
	}

	[Fact]
	public void Format_KoGame_ResolvedAndPlayed_ShowsTeamsAndScore()
	{
		var c = BuildMiniWc(simulateFinal: true, homeWinsFinal: true);
		c.Games[2].Format().Should().Be("[3] 28.06.2026 20:00 MEX 1-0 ARG  [final]");
	}

	[Fact]
	public void Format_KoGame_ExtraTime_AppendsAet()
	{
		var et = new KoGame
		{
			Id = 60, PlayedOn = new(2026, 7, 1, 20, 0, 0), RoundId = "final",
			HomeQual = "W57", AwayQual = "W58",
			HomeTeamId = "FRA", AwayTeamId = "ESP",
			Result = new Result(2, 1, GameEnd.EXTRA_TIME),
		};
		et.Format().Should().Be("[60] 01.07.2026 20:00 FRA 2-1 e.t. ESP  [final]");
	}

	[Fact]
	public void ToString_RecordDefault_IsVerboseDebugShape()
	{
		// Sanity: auto-ToString stays as record default (the "repr"); useful
		// in debugger / xUnit failure output where we want full state visible.
		var c = BuildMiniWc();
		var groupGame = c.Games[0];
		groupGame.ToString().Should().StartWith("GroupGame { ")
			.And.Contain("Id = 1").And.Contain("GroupLetter = A").And.Contain("HomeTeamId = MEX");
	}
}
