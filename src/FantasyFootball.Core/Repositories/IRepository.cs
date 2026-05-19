namespace FantasyFootball.Repositories;

public interface IRepository : IDisposable

{
	List<T> GetAll<T>() where T : NamedUniqueId, new();
	Task<List<T>> GetAllAsync<T>() where T : NamedUniqueId, new();
	T? Get<T>(int id) where T : NamedUniqueId, new();
	int Count<T>() where T : NamedUniqueId, new();

	void Save<T>(T item) where T : NamedUniqueId, new();

	/// <summary>
	/// Bulk-save many items at once. Default impl loops <see cref="Save{T}"/>; implementations
	/// that benefit from batching (e.g. LocalStorage, where each <c>Save</c> reserializes the
	/// whole bucket) override this to persist once.
	/// </summary>
	void SaveAll<T>(IEnumerable<T> items) where T : NamedUniqueId, new()
	{
		foreach (var item in items) { Save(item); }
	}

	void Delete<T>(T item) where T : NamedUniqueId, new();

	void Reset();
}
