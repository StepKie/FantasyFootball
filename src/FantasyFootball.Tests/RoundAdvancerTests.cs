namespace FantasyFootball.Tests;

public class RoundAdvancerTests(ITestOutputHelper output) : BaseTest(output, level: LogEventLevel.Debug)
{
	[Fact]
	public void ExpandedWorldCupFormat_AssignsEightDistinctTeamsAcrossAllSlots()
	{
		var format = ExpandedWorldCupFormat.Instance;
		var stage = BuildDeterministicStageWithControlled3rdPlaces(format.GroupCount);

		var assignments = format.ThirdPlaceSlotConstraints
			.ToDictionary(slot => slot, slot => format.ResolveThirdPlaceQualifier(stage, slot));

		assignments.Values.Should().OnlyHaveUniqueItems("each R32 third-place slot must be filled by a different team");
		assignments.Should().HaveCount(format.AdvancingThirdPlaceCount);

		foreach (var (slot, team) in assignments)
		{
			var allowedLetters = slot.Split('/');
			// Team names are constructed as "{letter}{position}" so the first char is the group letter.
			var groupLetter = team.ShortName[0].ToString();
			allowedLetters.Should().Contain(groupLetter, $"team for slot {slot} must come from one of the allowed groups");
		}
	}

	[Fact]
	public void ExpandedWorldCupFormat_OnlyTopEightThirdPlaceTeamsAdvance()
	{
		var format = ExpandedWorldCupFormat.Instance;
		var stage = BuildDeterministicStageWithControlled3rdPlaces(format.GroupCount);

		var thirdPlaceRanked = stage.Groups
			.Select(g => g.GetStandings()[2])
			.OrderByDescending(r => r)
			.ToList();
		var topEight = thirdPlaceRanked.Take(format.AdvancingThirdPlaceCount).Select(r => r.Team).ToList();
		var bottomFour = thirdPlaceRanked.Skip(format.AdvancingThirdPlaceCount).Select(r => r.Team).ToList();

		var assignedTeams = format.ThirdPlaceSlotConstraints
			.Select(slot => format.ResolveThirdPlaceQualifier(stage, slot))
			.ToList();

		assignedTeams.Should().BeSubsetOf(topEight, "only the top 8 third-place finishers may advance");
		assignedTeams.Should().NotIntersectWith(bottomFour, "the worst 4 third-place finishers must be eliminated");
	}

	[Fact]
	public async Task ExpandedWorldCupFormat_50RandomizedRuns_AllR32SlotsFilledWithCanonicalThirdPlace()
	{
		// Randomised property test: 50 independent WC 2026 simulations. For each run,
		// verify the R32 has 32 distinct non-placeholder teams AND the 8 third-place
		// qualifiers are exactly the top-8 third-place finishers by standings tiebreak.
		// Greedy slot-fill heuristics can succeed on most random inputs but fail on
		// adversarial standings (the live bug surfaced as TBD placeholders in the
		// R32). 50 independent random runs explore enough of the standings space to
		// hit failure cases the deterministic fixture-style tests miss.
		const int Runs = 50;
		for (var run = 0; run < Runs; run++)
		{
			var wm = CompetitionFactory.Default(CompetitionType.WM, DataService, 2026).Create();
			var simulator = new CompetitionSimulator(wm, Repo);
			await simulator.SimulateStage(wm.Stages[0]);

			var r32 = wm.Stages[1].Rounds.First();
			var participants = r32.KoGames.SelectMany<KoGame, Team>(g => [g.HomeTeam, g.AwayTeam]).ToList();

			participants.Should().HaveCount(32, $"run {run}: R32 has 32 slots");
			participants.Should().NotContain(t => t.Type == TeamType.PLACEHOLDER, $"run {run}: every R32 slot must resolve to a real team");
			participants.Should().OnlyHaveUniqueItems($"run {run}: each R32 slot must be a distinct team");

			// Canonical-advancer property: the 8 third-place qualifiers must be exactly
			// the top-8 third-place finishers (by points → GD → GF, the standing's
			// IComparable order), and the bottom-4 must NOT appear.
			var thirdPlaceRanked = wm.Groups
				.Select(g => g.GetStandings()[2])
				.OrderByDescending(r => r)
				.ToList();
			var topEight = thirdPlaceRanked.Take(8).Select(r => r.Team).ToHashSet();
			var bottomFour = thirdPlaceRanked.Skip(8).Select(r => r.Team).ToHashSet();

			var thirdPlaceAdvancers = r32.KoGames
				.SelectMany<KoGame, Qualifier>(g => [g.HomeQualifier, g.AwayQualifier])
				.OfType<GroupQualifier>()
				.Where(q => q.FinalPlacement == 3)
				.Select(q => q.Get()!)
				.ToList();
			thirdPlaceAdvancers.Should().HaveCount(8, $"run {run}: R32 has 8 third-place slots");
			thirdPlaceAdvancers.Should().OnlyContain(t => topEight.Contains(t), $"run {run}: only the 8 best third-place finishers advance");
			thirdPlaceAdvancers.Should().NotContain(t => bottomFour.Contains(t), $"run {run}: the 4 worst third-place finishers must not advance");
		}
	}

	[Fact]
	public async Task ExpandedWorldCupFormat_FullSimulationProducesDistinctR32Teams()
	{
		// End-to-end: every simulated 2026 tournament must produce a R32 with 32 unique participants.
		// Regression for: NamedUniqueId.Equals returns false for unsaved records (Id==0), so prior
		// implementations that removed picks via List<T>.Remove silently kept duplicates.
		var wm = InitCompetition(CompetitionType.WM, 2026);
		var simulator = new CompetitionSimulator(wm, Repo);
		await simulator.SimulateStage(wm.Stages[0]);

		var roundOf32 = wm.Stages[1].Rounds.First();
		var participants = roundOf32.KoGames.SelectMany<KoGame, Team>(g => [g.HomeTeam, g.AwayTeam]).ToList();

		participants.Should().HaveCount(32);
		participants.Should().OnlyHaveUniqueItems("R32 must contain 32 distinct teams");
	}

	[Fact]
	public void EuroFormat_AssignsFourDistinctTeamsAcrossR16ThirdPlaceSlots()
	{
		var format = EuroFormat.Instance;
		var stage = BuildDeterministicStageWithControlled3rdPlaces(format.GroupCount);

		var assignments = format.ThirdPlaceSlotConstraints
			.ToDictionary(slot => slot, slot => format.ResolveThirdPlaceQualifier(stage, slot));

		assignments.Values.Should().OnlyHaveUniqueItems("each Euro R16 third-place slot must be filled by a different team");
		assignments.Should().HaveCount(format.AdvancingThirdPlaceCount);
	}

	[Fact]
	public void WorldCupFormat_DoesNotSupportThirdPlaceAdvancement()
	{
		var format = WorldCupFormat.Instance;
		format.AdvancingThirdPlaceCount.Should().Be(0);
		format.ThirdPlaceSlotConstraints.Should().BeEmpty();

		Action act = () => format.ResolveThirdPlaceQualifier(new Stage(), "A/B/C");
		act.Should().Throw<NotSupportedException>();
	}

	[Fact]
	public void TournamentFormatRegistry_MapsGroupCountToCorrectFormat()
	{
		TournamentFormatRegistry.ForGroupCount(6).Should().BeSameAs(EuroFormat.Instance);
		TournamentFormatRegistry.ForGroupCount(8).Should().BeSameAs(WorldCupFormat.Instance);
		TournamentFormatRegistry.ForGroupCount(12).Should().BeSameAs(ExpandedWorldCupFormat.Instance);
	}

	/// <summary>
	/// Builds a Stage with the given number of groups (4 teams each) and pre-finished games
	/// so that each group has a uniquely-identifiable third-place team. The 3rd-place team's
	/// goal-difference varies by group index, providing a deterministic tie-break ranking.
	/// </summary>
	Stage BuildDeterministicStageWithControlled3rdPlaces(int groupCount)
	{
		var stage = new Stage { Name = "Group Stage" };
		var round = new Round { Name = "Round 1" };
		stage.Rounds.Add(round);

		for (int gi = 0; gi < groupCount; gi++)
		{
			var letter = "ABCDEFGHIJKL"[gi];
			var group = new Group { Name = $"Group {letter}", Stage = stage };
			var t1 = new Team { Name = $"{letter}1", ShortName = $"{letter}1" };
			var t2 = new Team { Name = $"{letter}2", ShortName = $"{letter}2" };
			var t3 = new Team { Name = $"{letter}3", ShortName = $"{letter}3" };
			var t4 = new Team { Name = $"{letter}4", ShortName = $"{letter}4" };
			group.Teams.AddRange([t1, t2, t3, t4]);
			stage.Groups.Add(group);

			// Goals scored by t3 vary so 3rd-place GD differs across groups.
			var t3Goals = groupCount - gi;

			round.RegularGames.AddRange(
			[
				FinishedGame(t1, t2, 2, 0, round),
				FinishedGame(t3, t4, t3Goals, 0, round),
				FinishedGame(t1, t3, 1, 0, round),
				FinishedGame(t2, t4, 1, 0, round),
				FinishedGame(t1, t4, 1, 0, round),
				FinishedGame(t2, t3, 0, 0, round),
			]);
		}

		return stage;
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
}
