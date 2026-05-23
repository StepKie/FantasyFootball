using System.Text.Json;
using Blazored.LocalStorage;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;

namespace FantasyFootball.Web.Services;

/// <summary>
/// LocalStorage-backed flat-competition store. Each competition lives in
/// its own key (<c>ff-flat:c:{id}</c>) so reads / writes / deletes touch
/// O(1) bytes; a small index key (<c>ff-flat:index</c>) lists the live
/// IDs so <see cref="GetAllAsync"/> and <see cref="CountAsync"/> don't
/// need to scan the entire LocalStorage namespace.
///
/// JSON shape mirrors <see cref="InMemoryCompetitionRepository"/>:
/// compact serialization via <see cref="FlatJson.Serialize"/>, parsed
/// back via <see cref="CompetitionDefinitionLoader.Load"/>. The
/// JSON round-trip is covered by the InMemory tests.
/// </summary>
public sealed class LocalStorageCompetitionRepository : ICompetitionRepository
{
	const string IndexKey = "ff-flat:index";
	const string CompetitionKeyPrefix = "ff-flat:c:";

	readonly ILocalStorageService _storage;

	// Monotonic id counter incremented synchronously before any await so two concurrent SaveAsync calls (bulk sim + user click) can't allocate the same id.
	int _nextId;

	public LocalStorageCompetitionRepository(ILocalStorageService storage)
	{
		_storage = storage;
	}

	public async Task<int> SaveAsync(Competition competition)
	{
		if (_nextId == 0)
		{
			var initIndex = await ReadIndex();
			if (initIndex.Count > 0) { _nextId = initIndex.Max(); }
		}
		if (competition.Id == 0)
		{
			competition.Id = ++_nextId;
		}

		// Index-first, payload-second: re-read before WriteIndex to avoid clobbering a concurrent save; orphan index is recoverable, missing payload isn't.
		var index = await ReadIndex();
		if (!index.Contains(competition.Id))
		{
			index.Add(competition.Id);
			await WriteIndex(index);
		}

		await _storage.SetItemAsStringAsync(KeyFor(competition.Id), FlatJson.Serialize(competition, compact: true));

		return competition.Id;
	}

	public async Task<Competition?> GetAsync(int id)
	{
		var json = await _storage.GetItemAsStringAsync(KeyFor(id));
		if (string.IsNullOrEmpty(json)) { return null; }
		var c = CompetitionDefinitionLoader.Load(json);
		c.Id = id;
		return c;
	}

	public async Task<IReadOnlyList<Competition>> GetAllAsync()
	{
		var index = await ReadIndex();
		var result = new List<Competition>(index.Count);
		foreach (var id in index)
		{
			var c = await GetAsync(id);
			if (c is not null) { result.Add(c); }
		}

		return result;
	}

	public async Task DeleteAsync(int id)
	{
		await _storage.RemoveItemAsync(KeyFor(id));
		var index = await ReadIndex();
		if (index.Remove(id))
		{
			await WriteIndex(index);
		}
	}

	public async Task<int> CountAsync() => (await ReadIndex()).Count;

	public async Task ResetAsync()
	{
		var index = await ReadIndex();
		foreach (var id in index)
		{
			await _storage.RemoveItemAsync(KeyFor(id));
		}

		await _storage.RemoveItemAsync(IndexKey);
		_nextId = 0;
	}

	async Task<List<int>> ReadIndex()
	{
		var raw = await _storage.GetItemAsStringAsync(IndexKey);
		if (string.IsNullOrEmpty(raw)) { return []; }
		return JsonSerializer.Deserialize<List<int>>(raw) ?? [];
	}

	async Task WriteIndex(List<int> ids) =>
		await _storage.SetItemAsStringAsync(IndexKey, JsonSerializer.Serialize(ids));

	static string KeyFor(int id) => $"{CompetitionKeyPrefix}{id}";
}
