using FantasyFootball.Repositories;

namespace FantasyFootball.Tests;

public class HeadToHeadLookupTests
{
	[Fact]
	public void Across_NoCompetitions_ReturnsZeros()
	{
		var repo = new FakeRepo();
		var teamA = new Team { Name = "A" };
		var teamB = new Team { Name = "B" };

		var result = HeadToHeadLookup.Across(repo, teamA, teamB);

		result.Should().Be(new HeadToHeadLookup.Result(0, 0, 0));
		result.Total.Should().Be(0);
	}

	[Fact]
	public void Across_CountsWinsDrawsAndLosses_AcrossMultipleCompetitions()
	{
		var repo = new FakeRepo
		{
			Competitions =
			{
				// First comp: A beats B 2-1, then they draw 1-1.
				BuildComp(("A", "B", 2, 1), ("B", "A", 1, 1)),
				// Second comp: B beats A 2-0. Game between A and C is irrelevant — should not count.
				BuildComp(("A", "B", 0, 2), ("A", "C", 3, 0)),
			}
		};

		var result = HeadToHeadLookup.Across(repo, new Team { Name = "A" }, new Team { Name = "B" });

		result.TeamAWins.Should().Be(1);
		result.Draws.Should().Be(1);
		result.TeamBWins.Should().Be(1);
		result.Total.Should().Be(3);
	}

	[Fact]
	public void Across_DirectionAgnostic_TeamAHomeOrAwayBothCountForTeamAWins()
	{
		var repo = new FakeRepo
		{
			Competitions =
			{
				BuildComp(
					("A", "B", 1, 0),  // A wins as home
					("B", "A", 0, 1)),  // A wins as away
			}
		};

		var result = HeadToHeadLookup.Across(repo, new Team { Name = "A" }, new Team { Name = "B" });

		result.TeamAWins.Should().Be(2);
		result.TeamBWins.Should().Be(0);
		result.Draws.Should().Be(0);
	}

	[Fact]
	public void Across_IgnoresUnfinishedGames()
	{
		var repo = new FakeRepo
		{
			Competitions =
			{
				BuildComp(
					("A", "B", 1, 0),  // finished
					new ScheduledGame("A", "B")),  // not yet played
			}
		};

		var result = HeadToHeadLookup.Across(repo, new Team { Name = "A" }, new Team { Name = "B" });

		result.TeamAWins.Should().Be(1);
		result.Total.Should().Be(1);
	}

	// Each comp builds its own fresh Team instances per name — mirrors the LocalStorage path where every deserialized
	// Competition graph has its own Team objects (NamedUniqueId.Equals would not match them; name-based matching does).
	static Competition BuildComp(params object[] entries)
	{
		var round = new Round { Name = "Round 1" };
		var teams = new Dictionary<string, Team>(StringComparer.Ordinal);
		Team Get(string name) => teams.TryGetValue(name, out var t) ? t : teams[name] = new Team { Name = name };

		foreach (var entry in entries)
		{
			switch (entry)
			{
				case (string home, string away, int homeScore, int awayScore):
					round.RegularGames.Add(new Game
					{
						HomeTeam = Get(home),
						AwayTeam = Get(away),
						HomeScore = homeScore,
						AwayScore = awayScore,
						State = GameState.FINISHED,
						Round = round,
					});
					break;
				case ScheduledGame sg:
					round.RegularGames.Add(new Game
					{
						HomeTeam = Get(sg.Home),
						AwayTeam = Get(sg.Away),
						State = GameState.SCHEDULED,
						Round = round,
					});
					break;
				default:
					throw new ArgumentException($"Unsupported entry type {entry.GetType()}");
			}
		}

		var stage = new Stage { Name = "Group Stage", Rounds = [round] };
		round.Stage = stage;
		var comp = new Competition { Name = "test", ShortName = "T", Stages = [stage] };
		stage.Competition = comp;

		return comp;
	}

	record ScheduledGame(string Home, string Away);

	sealed class FakeRepo : IRepository
	{
		public List<Competition> Competitions { get; init; } = [];

		public List<T> GetAll<T>() where T : NamedUniqueId, new()
			=> typeof(T) == typeof(Competition) ? Competitions.Cast<T>().ToList() : [];

		public Task<List<T>> GetAllAsync<T>() where T : NamedUniqueId, new() => Task.FromResult(GetAll<T>());
		public T? Get<T>(int id) where T : NamedUniqueId, new() => null;
		public int Count<T>() where T : NamedUniqueId, new() => GetAll<T>().Count;
		public void Save<T>(T item) where T : NamedUniqueId, new() { }
		public void Delete<T>(T item) where T : NamedUniqueId, new() { }
		public void Reset() { }
		public void Dispose() { }
	}
}
