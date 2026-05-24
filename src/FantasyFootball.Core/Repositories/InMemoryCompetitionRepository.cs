using FantasyFootball.Models;
using FantasyFootball.Services;

namespace FantasyFootball.Repositories;

/// <summary>
/// In-process flat-competition store. Serializes via
/// <see cref="FlatJson.Serialize"/> on save and re-materializes via
/// <see cref="CompetitionDefinitionLoader.Load"/> on get — same JSON
/// path the on-disk persistence will use, so any serialization bug surfaces
/// in unit tests rather than in the browser.
///
/// Thread-safety: not safe for concurrent mutation. The bulk-sim runner
/// drives this from a single thread today.
/// </summary>
public sealed class InMemoryCompetitionRepository : ICompetitionRepository
{
	readonly Dictionary<int, string> _storage = [];
	int _nextId = 1;

	public Task<int> SaveAsync(Competition competition)
	{
		if (competition.Id == 0)
		{
			competition.Id = _nextId++;
		}
		else if (competition.Id >= _nextId)
		{
			// Caller is restoring with an explicit ID (e.g. import). Bump
			// the cursor so we don't later hand out a colliding ID.
			_nextId = competition.Id + 1;
		}
		// Compact JSON for runtime persistence — every byte saved matters
		// once LocalStorage / IndexedDB are wired in, and these blobs are
		// machine-only (humans read the indented definition files instead).
		_storage[competition.Id] = FlatJson.Serialize(competition, compact: true);
		return Task.FromResult(competition.Id);
	}

	public Task<Competition?> GetAsync(int id)
	{
		if (!_storage.TryGetValue(id, out var json))
		{
			return Task.FromResult<Competition?>(null);
		}
		var c = CompetitionDefinitionLoader.Load(json);
		c.Id = id;
		return Task.FromResult<Competition?>(c);
	}

	public Task<IReadOnlyList<Competition>> GetAllAsync()
	{
		var list = new List<Competition>(_storage.Count);
		foreach (var (id, json) in _storage)
		{
			var c = CompetitionDefinitionLoader.Load(json);
			c.Id = id;
			list.Add(c);
		}
		return Task.FromResult<IReadOnlyList<Competition>>(list);
	}

	public Task DeleteAsync(int id)
	{
		_storage.Remove(id);
		return Task.CompletedTask;
	}

	public Task<int> CountAsync() => Task.FromResult(_storage.Count);

	public Task ResetAsync()
	{
		_storage.Clear();
		_nextId = 1;
		return Task.CompletedTask;
	}
}
