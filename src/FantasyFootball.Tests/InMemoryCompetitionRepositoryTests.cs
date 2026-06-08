using FantasyFootball.Repositories;

namespace FantasyFootball.Tests;

/// <summary>
/// Exercises the persistence contract on the in-memory implementation —
/// also indirectly verifies the JSON serialize/load round-trip for
/// Competition, since the store routes through both on every save
/// and load.
/// </summary>
public class InMemoryCompetitionRepositoryTests
{
	readonly EmbeddedCompetitionDefinitionStore _definitions = new();
	readonly InMemoryCompetitionRepository _repo = new();

	[Fact]
	public async Task Save_FreshCompetition_AssignsId()
	{
		var c = _definitions.Load("wm-2022");
		c.Id.Should().Be(0);

		var id = await _repo.SaveAsync(c);

		id.Should().BeGreaterThan(0);
		c.Id.Should().Be(id);
	}

	[Fact]
	public async Task Save_TwoFresh_AssignsDistinctIds()
	{
		var a = _definitions.Load("wm-2022");
		var b = _definitions.Load("em-2024");

		var idA = await _repo.SaveAsync(a);
		var idB = await _repo.SaveAsync(b);

		idA.Should().NotBe(idB);
		(await _repo.CountAsync()).Should().Be(2);
	}

	[Fact]
	public async Task Get_AfterSave_ReturnsCompetitionWithSameContent()
	{
		var saved = _definitions.Load("wm-2022");
		await _repo.SaveAsync(saved);

		var loaded = await _repo.GetAsync(saved.Id);

		loaded.Should().NotBeNull();
		loaded!.Id.Should().Be(saved.Id);
		loaded.DefinitionId.Should().Be(saved.DefinitionId);
		loaded.Games.Should().HaveCount(saved.Games.Length);
		loaded.GroupAssignments.Should().BeEquivalentTo(saved.GroupAssignments);
	}

	[Fact]
	public async Task Get_UnknownId_ReturnsNull()
	{
		var c = await _repo.GetAsync(9999);
		c.Should().BeNull();
	}

	[Fact]
	public async Task Save_ExistingId_OverwritesInPlace()
	{
		var c = _definitions.Load("wm-2022");
		var id = await _repo.SaveAsync(c);

		// Mutate simulation state, persist again under the same ID
		var start = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
		var finish = new DateTime(2026, 5, 1, 12, 5, 30, DateTimeKind.Utc);
		c.SimulationStart = start;
		c.SimulationFinished = finish;
		await _repo.SaveAsync(c);

		var loaded = await _repo.GetAsync(id);
		loaded!.SimulationStart.Should().Be(start);
		loaded.SimulationFinished.Should().Be(finish);
		(await _repo.CountAsync()).Should().Be(1, "overwriting in place should not grow the store");
	}

	[Fact]
	public async Task RoundTrip_PreservesGameResults()
	{
		// Populate results on a few games — verify they survive the
		// serialize-deserialize cycle through SaveAsync/GetAsync.
		var c = _definitions.Load("wm-2022");
		var g1 = (GroupGame)c.Games[0];
		g1.Result = new Result(2, 1, GameEnd.NORMAL);
		var g2 = (GroupGame)c.Games[1];
		g2.Result = new Result(0, 0, GameEnd.NORMAL);

		var id = await _repo.SaveAsync(c);
		var loaded = await _repo.GetAsync(id);

		var l1 = (GroupGame)loaded!.Games[0];
		l1.Result.Should().Be(new Result(2, 1, GameEnd.NORMAL));
		var l2 = (GroupGame)loaded.Games[1];
		l2.Result.Should().Be(new Result(0, 0, GameEnd.NORMAL));
	}

	[Fact]
	public async Task RoundTrip_PreservesResolvedKoQualifiers()
	{
		// KO games carry HomeTeamId / AwayTeamId as cached qualifier
		// resolution. Both must survive persistence.
		var c = _definitions.Load("wm-2022");
		var ko = c.Games.OfType<KoGame>().First();
		ko.HomeTeamId = "NED";
		ko.AwayTeamId = "USA";
		ko.Result = new Result(3, 1, GameEnd.NORMAL);

		var id = await _repo.SaveAsync(c);
		var loaded = await _repo.GetAsync(id);

		var lko = loaded!.Games.OfType<KoGame>().First(g => g.Id == ko.Id);
		lko.HomeTeamId.Should().Be("NED");
		lko.AwayTeamId.Should().Be("USA");
		lko.Result.Should().Be(new Result(3, 1, GameEnd.NORMAL));
	}

	[Fact]
	public async Task GetAll_ReturnsEveryPersistedCompetition()
	{
		await _repo.SaveAsync(_definitions.Load("wm-2022"));
		await _repo.SaveAsync(_definitions.Load("wm-2026"));
		await _repo.SaveAsync(_definitions.Load("em-2024"));

		var all = await _repo.GetAllAsync();
		all.Should().HaveCount(3);
		all.Select(c => c.DefinitionId).Should().BeEquivalentTo(["wm-2022", "wm-2026", "em-2024"]);
	}

	[Fact]
	public async Task GetAllSummaries_FaithfullyProjectsEveryPersistedCompetition()
	{
		var wm = _definitions.Load("wm-2022");
		await _repo.SaveAsync(wm);
		await _repo.SaveAsync(_definitions.Load("em-2024"));

		var summaries = await _repo.GetAllSummariesAsync();

		summaries.Should().HaveCount(2);
		var wmSummary = summaries.Single(s => s.Id == wm.Id);
		wmSummary.Type.Should().Be(wm.Type);
		wmSummary.Title.Should().Be(wm.Title);
		wmSummary.IsNationalTeam.Should().BeTrue();
		wmSummary.TotalGames.Should().Be(wm.Games.Length);
		wmSummary.PlayedGames.Should().Be(wm.Games.Count(g => g.Result is not null));
		wmSummary.IsFinished.Should().Be(wm.IsFinished());
		wmSummary.WinnerId.Should().Be(wm.WinnerTeamId());
	}

	[Fact]
	public async Task Delete_RemovesById()
	{
		var c = _definitions.Load("wm-2022");
		await _repo.SaveAsync(c);

		await _repo.DeleteAsync(c.Id);

		(await _repo.GetAsync(c.Id)).Should().BeNull();
		(await _repo.CountAsync()).Should().Be(0);
	}

	[Fact]
	public async Task Delete_UnknownId_IsNoOp()
	{
		await _repo.SaveAsync(_definitions.Load("wm-2022"));
		Func<Task> act = () => _repo.DeleteAsync(9999);
		await act.Should().NotThrowAsync();
		(await _repo.CountAsync()).Should().Be(1);
	}
}
