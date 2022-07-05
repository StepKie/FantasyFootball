namespace FantasyFootball.Repositories;

public interface IRepository : IDisposable

{
	List<T> GetAll<T>() where T : NamedUniqueId, new();
	Task<List<T>> GetAllAsync<T>() where T : NamedUniqueId, new();
	T? Get<T>(int id) where T : NamedUniqueId, new();
	int Count<T>() where T : NamedUniqueId, new();

	void Save<T>(T item) where T : NamedUniqueId, new();

	void Delete<T>(T item) where T : NamedUniqueId, new();

	void Reset();
}
