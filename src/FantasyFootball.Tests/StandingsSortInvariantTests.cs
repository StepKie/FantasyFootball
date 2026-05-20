namespace FantasyFootball.Tests;

/// <summary>
/// Simulates the group stage many times across WC32 / WC48 / EM24 and asserts every
/// group's standings are non-increasing on Points → GoalDifference → GoalsFor.
/// H2H tie-break is a follow-up; this test only pins the strict-prefix invariant.
/// </summary>
public class StandingsSortInvariantTests(ITestOutputHelper output) : BaseTest(output, level: LogEventLevel.Warning)
{
	[InlineData(CompetitionType.WM, 2022)] // WC32
	[InlineData(CompetitionType.WM, 2026)] // WC48
	[InlineData(CompetitionType.EM, 2024)] // EM24
	[Theory]
	public async Task GroupStandings_AreOrderedCorrectly(CompetitionType type, int year)
	{
		const int iterations = 20;
		var violations = new List<string>();

		for (int i = 0; i < iterations; i++)
		{
			var competition = InitCompetition(type, year);
			var simulator = new CompetitionSimulator(competition, Repo);
			await simulator.SimulateStage(competition.Stages[0]);

			foreach (var group in competition.Groups)
			{
				var standings = group.GetStandings();
				for (int j = 0; j < standings.Count - 1; j++)
				{
					var a = standings[j];
					var b = standings[j + 1];
					if (a.Points < b.Points)
					{
						violations.Add($"[iter {i}] {type} {year} {group.Name}: pos {j + 1} {a.Team.ShortName}(P={a.Points}) above pos {j + 2} {b.Team.ShortName}(P={b.Points})");
					}
					else if (a.Points == b.Points && a.GoalDifference < b.GoalDifference)
					{
						violations.Add($"[iter {i}] {type} {year} {group.Name}: pos {j + 1} {a.Team.ShortName}(P={a.Points},GD={a.GoalDifference}) above pos {j + 2} {b.Team.ShortName}(P={b.Points},GD={b.GoalDifference})");
					}
					else if (a.Points == b.Points && a.GoalDifference == b.GoalDifference && a.GoalsFor < b.GoalsFor)
					{
						violations.Add($"[iter {i}] {type} {year} {group.Name}: pos {j + 1} {a.Team.ShortName}(P={a.Points},GD={a.GoalDifference},GF={a.GoalsFor}) above pos {j + 2} {b.Team.ShortName}(P={b.Points},GD={b.GoalDifference},GF={b.GoalsFor})");
					}
				}
			}

			Repo.Delete(competition);
		}

		violations.Should().BeEmpty($"all groups must sort by Pts DESC → GD DESC → GF DESC\n  - {string.Join("\n  - ", violations)}");
	}

	/// <summary>
	/// Storage round-trip variant: serialize → deserialize → re-check sort.
	/// If the bug is in stored-game reconstruction (Team references not rewired)
	/// rather than the live sim, this catches it.
	/// </summary>
	[InlineData(CompetitionType.WM, 2026)]
	[Theory]
	public async Task GroupStandings_AreOrderedCorrectly_AfterStorageRoundTrip(CompetitionType type, int year)
	{
		const int iterations = 20;
		var violations = new List<string>();

		for (int i = 0; i < iterations; i++)
		{
			var competition = InitCompetition(type, year);
			var simulator = new CompetitionSimulator(competition, Repo);
			await simulator.SimulateStage(competition.Stages[0]);
			Repo.Save(competition);

			var reloaded = Repo.Get<Competition>(competition.Id)!;
			foreach (var group in reloaded.Groups)
			{
				var standings = group.GetStandings();
				for (int j = 0; j < standings.Count - 1; j++)
				{
					var a = standings[j];
					var b = standings[j + 1];
					if (a.Points < b.Points)
					{
						violations.Add($"[iter {i}] {type} {year} {group.Name} (post-roundtrip): pos {j + 1} {a.Team.ShortName}(P={a.Points}) above pos {j + 2} {b.Team.ShortName}(P={b.Points})");
					}
					else if (a.Points == b.Points && a.GoalDifference < b.GoalDifference)
					{
						violations.Add($"[iter {i}] {type} {year} {group.Name} (post-roundtrip): pos {j + 1} {a.Team.ShortName}(P={a.Points},GD={a.GoalDifference}) above pos {j + 2} {b.Team.ShortName}(P={b.Points},GD={b.GoalDifference})");
					}
					else if (a.Points == b.Points && a.GoalDifference == b.GoalDifference && a.GoalsFor < b.GoalsFor)
					{
						violations.Add($"[iter {i}] {type} {year} {group.Name} (post-roundtrip): pos {j + 1} {a.Team.ShortName}(P={a.Points},GD={a.GoalDifference},GF={a.GoalsFor}) above pos {j + 2} {b.Team.ShortName}(P={b.Points},GD={b.GoalDifference},GF={b.GoalsFor})");
					}
				}
			}

			Repo.Delete(reloaded);
		}

		violations.Should().BeEmpty($"sort invariant must survive storage round-trip\n  - {string.Join("\n  - ", violations)}");
	}
}
