using FantasyFootball.Repositories;
using FantasyFootball.Services;

namespace FantasyFootball.Tests;

/// <summary>
/// Covers the cross-competition H2H tally surface that backs the
/// game-details popover —
/// <c>AcrossAsync(ICompetitionRepository, string, string)</c>.
/// </summary>
public class HeadToHeadLookupTests
{
	readonly InMemoryCompetitionRepository _repo = new();

	[Fact]
	public async Task AcrossAsync_NoCompetitions_ReturnsZeros()
	{
		var result = await HeadToHeadLookup.AcrossAsync(_repo, "MEX", "BRA");

		result.Should().Be(new HeadToHeadLookup.Result(0, 0, 0));
		result.Total.Should().Be(0);
	}

	[Fact]
	public async Task AcrossAsync_GamesBetweenOtherTeams_ReturnsZeros()
	{
		await _repo.SaveAsync(SingleGame("MEX", "BRA", 1, 0));
		// Query a different pair — should ignore the stored MEX vs BRA game.
		var result = await HeadToHeadLookup.AcrossAsync(_repo, "ARG", "GER");

		result.Total.Should().Be(0);
	}

	[Fact]
	public async Task AcrossAsync_HomeWin_CountsAWin()
	{
		await _repo.SaveAsync(SingleGame("MEX", "BRA", 2, 1));

		var result = await HeadToHeadLookup.AcrossAsync(_repo, "MEX", "BRA");

		result.Should().Be(new HeadToHeadLookup.Result(TeamAWins: 1, Draws: 0, TeamBWins: 0));
	}

	[Fact]
	public async Task AcrossAsync_AwayWin_CountsBWin()
	{
		await _repo.SaveAsync(SingleGame("MEX", "BRA", 0, 2));

		var result = await HeadToHeadLookup.AcrossAsync(_repo, "MEX", "BRA");

		result.Should().Be(new HeadToHeadLookup.Result(TeamAWins: 0, Draws: 0, TeamBWins: 1));
	}

	[Fact]
	public async Task AcrossAsync_PairOrderSymmetric_AWinFromTeamAPerspective_WhenAwayAtBsHome()
	{
		await _repo.SaveAsync(SingleGame("BRA", "MEX", 0, 2));

		var result = await HeadToHeadLookup.AcrossAsync(_repo, "MEX", "BRA");

		result.Should().Be(new HeadToHeadLookup.Result(TeamAWins: 1, Draws: 0, TeamBWins: 0));
	}

	[Fact]
	public async Task AcrossAsync_PairOrderSymmetric_BWinFromTeamAPerspective()
	{
		// Home team is teamB (BRA), away is teamA (MEX). BRA wins.
		await _repo.SaveAsync(SingleGame("BRA", "MEX", 3, 1));

		// Query as (MEX, BRA) — BRA's win counts as a B-win.
		var result = await HeadToHeadLookup.AcrossAsync(_repo, "MEX", "BRA");

		result.Should().Be(new HeadToHeadLookup.Result(TeamAWins: 0, Draws: 0, TeamBWins: 1));
	}

	[Fact]
	public async Task AcrossAsync_Draw_CountsDraw()
	{
		await _repo.SaveAsync(SingleGame("MEX", "BRA", 1, 1));

		var result = await HeadToHeadLookup.AcrossAsync(_repo, "MEX", "BRA");

		result.Should().Be(new HeadToHeadLookup.Result(TeamAWins: 0, Draws: 1, TeamBWins: 0));
	}

	[Fact]
	public async Task AcrossAsync_AggregatesAcrossCompetitions()
	{
		// Three separate competitions with one MEX vs BRA game each.
		await _repo.SaveAsync(SingleGame("MEX", "BRA", 2, 1));   // A win
		await _repo.SaveAsync(SingleGame("BRA", "MEX", 1, 1));   // Draw
		await _repo.SaveAsync(SingleGame("BRA", "MEX", 3, 0));   // B win

		var result = await HeadToHeadLookup.AcrossAsync(_repo, "MEX", "BRA");

		result.Should().Be(new HeadToHeadLookup.Result(TeamAWins: 1, Draws: 1, TeamBWins: 1));
		result.Total.Should().Be(3);
	}

	[Fact]
	public async Task AcrossAsync_UnplayedGame_NotCounted()
	{
		// Save a competition with the matchup but no Result (scheduled, not played).
		var comp = SingleGame("MEX", "BRA", 0, 0);
		((GroupGame)comp.Games[0]).Result = null;
		await _repo.SaveAsync(comp);

		var result = await HeadToHeadLookup.AcrossAsync(_repo, "MEX", "BRA");

		result.Total.Should().Be(0);
	}

	// Build a minimal Competition with a single played group game between the two teams.
	static Competition SingleGame(string home, string away, int homeScore, int awayScore) => new()
	{
		Title = "H2H Test", Type = CompetitionType.WM, Year = 2026,
		DefinitionId = "h2h-test", FormatId = "h2h",
		GroupAssignments = [[home, away]],
		Stages = [new Stage("group", "Group", 0)],
		Rounds = [new Round("g-r1", "Group Round 1", "group", 0)],
		Games =
		[
			new GroupGame
			{
				Id = 1, PlayedOn = new(2026, 6, 1, 16, 0, 0), RoundId = "g-r1",
				GroupLetter = "A", HomeTeamId = home, AwayTeamId = away,
				Result = new Result { HomeScore = homeScore, AwayScore = awayScore, Ending = GameEnd.NORMAL },
			},
		],
	};
}
