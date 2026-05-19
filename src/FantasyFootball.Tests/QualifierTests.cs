namespace FantasyFootball.Tests;

/// <summary>
/// Regression tests for the placeholder/early-resolve bugs discovered during the UI
/// polish pass — the KO bracket page renders qualifier placeholders before the group
/// stage finishes, which exposed two latent bugs:
///   1. <see cref="GameQualifier.GetPlaceholder"/> hardcoded "Winner" even when
///      <c>LoserQualifies</c> was true (third-place match showed the wrong sides).
///   2. <see cref="GroupQualifier.Get"/> tried to resolve the 3rd-place slot the
///      moment a single group finished, throwing because the cross-group
///      assignment needs every group's standings.
/// </summary>
public class QualifierTests
{
	[Fact]
	public void GameQualifier_Placeholder_UsesWinner_WhenWinnerQualifies()
	{
		var q = new GameQualifier { GameNoInCompetition = 5, LoserQualifies = false };
		q.GetPlaceholder().Name.Should().Be($"{Res.Winner} 5");
	}

	[Fact]
	public void GameQualifier_Placeholder_UsesLoser_WhenLoserQualifies()
	{
		var q = new GameQualifier { GameNoInCompetition = 5, LoserQualifies = true };
		q.GetPlaceholder().Name.Should().Be($"{Res.Loser} 5");
	}

	[Fact]
	public void GroupQualifier_ThirdPlace_ReturnsNull_WhenStageStillInProgress()
	{
		// One group finished, others still in progress. The 3rd-place qualifier on the
		// finished group must NOT try to resolve yet — cross-group comparison is invalid
		// while other groups are open.
		var qualifier = BuildQualifier(
			groupCount: 12,
			finishedGroupIndices: [0],
			groupId: 0,
			finalPlacement: 3,
			thirdPlaceCombination: "A/B/C/D/F");

		var act = () => qualifier.Get();
		act.Should().NotThrow();
		qualifier.Get().Should().BeNull();
	}

	[Fact]
	public void GroupQualifier_ThirdPlace_Resolves_WhenAllGroupsFinished()
	{
		var qualifier = BuildQualifier(
			groupCount: 12,
			finishedGroupIndices: Enumerable.Range(0, 12),
			groupId: 0,
			finalPlacement: 3,
			thirdPlaceCombination: "A/B/C/D/F");

		qualifier.Get().Should().NotBeNull();
	}

	[Fact]
	public void GroupQualifier_TopTwo_Resolves_WhenOwnGroupFinished()
	{
		// FinalPlacement 1 and 2 only depend on the qualifier's own group, so they
		// resolve as soon as that group finishes — independent of other groups.
		var first = BuildQualifier(groupCount: 12, finishedGroupIndices: [0], groupId: 0, finalPlacement: 1);
		var second = BuildQualifier(groupCount: 12, finishedGroupIndices: [0], groupId: 0, finalPlacement: 2);

		first.Get().Should().NotBeNull();
		second.Get().Should().NotBeNull();
	}

	/// <summary>
	/// Wires a GroupQualifier into a full Competition graph (group stage with
	/// <paramref name="groupCount"/> groups + KO stage holding the qualifier's
	/// owning KoGame), so the <c>Game.Round.Stage.Competition</c> chain
	/// <see cref="Qualifier.Competition"/> walks via resolves.
	/// </summary>
	static GroupQualifier BuildQualifier(
		int groupCount,
		IEnumerable<int> finishedGroupIndices,
		int groupId,
		int finalPlacement,
		string thirdPlaceCombination = "")
	{
		var competition = new Competition { Name = "Test", ShortName = "T", Type = CompetitionType.WM };

		var groupStage = new Stage { Name = "Group Stage", Competition = competition };
		competition.Stages.Add(groupStage);
		var groupRound = new Round { Name = "Round 1", Stage = groupStage };
		groupStage.Rounds.Add(groupRound);

		var finished = finishedGroupIndices.ToHashSet();
		for (var gi = 0; gi < groupCount; gi++)
		{
			var letter = "ABCDEFGHIJKL"[gi];
			var group = new Group { Name = $"Group {letter}", Stage = groupStage };
			var t1 = new Team { Name = $"{letter}1", ShortName = $"{letter}1" };
			var t2 = new Team { Name = $"{letter}2", ShortName = $"{letter}2" };
			var t3 = new Team { Name = $"{letter}3", ShortName = $"{letter}3" };
			var t4 = new Team { Name = $"{letter}4", ShortName = $"{letter}4" };
			group.Teams.AddRange([t1, t2, t3, t4]);
			groupStage.Groups.Add(group);

			if (finished.Contains(gi))
			{
				var t3Goals = groupCount - gi;
				groupRound.RegularGames.AddRange(
				[
					FinishedGame(t1, t2, 2, 0, groupRound),
					FinishedGame(t3, t4, t3Goals, 0, groupRound),
					FinishedGame(t1, t3, 1, 0, groupRound),
					FinishedGame(t2, t4, 1, 0, groupRound),
					FinishedGame(t1, t4, 1, 0, groupRound),
					FinishedGame(t2, t3, 0, 0, groupRound),
				]);
			}
			else
			{
				// Mirror the real-app shape: an unfinished group has SCHEDULED games, not
				// zero games. Without this, Group.IsFinished returns vacuously true and
				// the "stage in progress" assertion can't distinguish finished from empty.
				groupRound.RegularGames.AddRange(
				[
					ScheduledGame(t1, t2, groupRound),
					ScheduledGame(t3, t4, groupRound),
					ScheduledGame(t1, t3, groupRound),
					ScheduledGame(t2, t4, groupRound),
					ScheduledGame(t1, t4, groupRound),
					ScheduledGame(t2, t3, groupRound),
				]);
			}
		}

		var koStage = new Stage { Name = "K.O. Stage", Competition = competition };
		competition.Stages.Add(koStage);
		var koRound = new Round { Name = "Round of 32", Stage = koStage };
		koStage.Rounds.Add(koRound);

		var qualifier = new GroupQualifier
		{
			GroupId = groupId,
			FinalPlacement = finalPlacement,
			ThirdPlaceCombination = thirdPlaceCombination,
		};
		var koGame = new KoGame { Round = koRound };
		qualifier.Game = koGame;
		koRound.KoGames.Add(koGame);

		return qualifier;
	}

	static Game FinishedGame(Team home, Team away, int homeScore, int awayScore, Round round) => new()
	{
		HomeTeam = home,
		AwayTeam = away,
		HomeScore = homeScore,
		AwayScore = awayScore,
		State = GameState.FINISHED,
		Ending = GameEnd.NORMAL,
		Round = round,
	};

	static Game ScheduledGame(Team home, Team away, Round round) => new()
	{
		HomeTeam = home,
		AwayTeam = away,
		State = GameState.SCHEDULED,
		Round = round,
	};
}
