namespace FantasyFootball.Repositories;

/// <summary>
/// In-process bucket-per-type implementation of <see cref="IRepository"/>.
/// Mirrors <see cref="LocalStorageRepository"/>'s runtime shape without any
/// backing store — used by tests and by the (retiring) MAUI host. Any
/// <see cref="NamedUniqueId"/> subtype is accepted automatically.
/// </summary>
public sealed class InMemoryRepository : IRepository
{
	readonly Dictionary<Type, Dictionary<int, NamedUniqueId>> _buckets = [];

	public List<T> GetAll<T>() where T : NamedUniqueId, new() => Bucket<T>().Values.Cast<T>().ToList();

	public Task<List<T>> GetAllAsync<T>() where T : NamedUniqueId, new() => Task.FromResult(GetAll<T>());

	public T? Get<T>(int id) where T : NamedUniqueId, new() => Bucket<T>().TryGetValue(id, out var item) ? (T)item : null;

	public int Count<T>() where T : NamedUniqueId, new() => Bucket<T>().Count;

	public void Save<T>(T item) where T : NamedUniqueId, new()
	{
		var bucket = Bucket<T>();
		if (item.Id == 0) { item.Id = bucket.Keys.DefaultIfEmpty(0).Max() + 1; }
		bucket[item.Id] = item;
	}

	public void SaveAll<T>(IEnumerable<T> items) where T : NamedUniqueId, new()
	{
		var bucket = Bucket<T>();
		var nextId = bucket.Keys.DefaultIfEmpty(0).Max() + 1;
		foreach (var item in items)
		{
			if (item.Id == 0) { item.Id = nextId++; }
			bucket[item.Id] = item;
		}
	}

	public void Delete<T>(T item) where T : NamedUniqueId, new() => Bucket<T>().Remove(item.Id);

	public void Reset() => _buckets.Clear();

	public void Dispose() { }

	Dictionary<int, NamedUniqueId> Bucket<T>() where T : NamedUniqueId, new()
	{
		if (!_buckets.TryGetValue(typeof(T), out var bucket))
		{
			bucket = [];
			_buckets[typeof(T)] = bucket;
		}
		return bucket;
	}
}
