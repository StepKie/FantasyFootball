using FantasyFootball.Repositories;

namespace FantasyFootball.Tests;

/// <summary>
/// Coverage for <see cref="Game"/>'s state transitions, in particular the
/// <see cref="Game.ClearResult"/> inverse used by the undo flow.
/// </summary>
public class GameTests : BaseTest
{
	public GameTests(ITestOutputHelper output) : base(output) { }

	[Fact]
	public void ClearResult_AfterSimulate_ResetsToScheduledWithZeroScores()
	{
		var competition = InitCompetition(CompetitionType.WM, 2022);
		var game = competition.CurrentGame!;
		game.Simulate();
		game.IsFinished.Should().BeTrue("setup: a freshly-simmed game should be finished");

		game.ClearResult();

		game.IsFinished.Should().BeFalse();
		game.State.Should().Be(GameState.SCHEDULED);
		game.HomeScore.Should().Be(0);
		game.AwayScore.Should().Be(0);
		game.Ending.Should().Be(GameEnd.NORMAL);
	}

	[Fact]
	public void Simulate_AfterClearResult_ProducesFinishedResultAgain()
	{
		var competition = InitCompetition(CompetitionType.WM, 2022);
		var game = competition.CurrentGame!;
		game.Simulate();
		game.ClearResult();
		game.IsReadyToStart.Should().BeTrue("cleared game with real teams should be ready to sim again");

		game.Simulate();

		game.IsFinished.Should().BeTrue();
	}
}
