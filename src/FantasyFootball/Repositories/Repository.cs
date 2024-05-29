using System.Reflection;

namespace FantasyFootball.Repositories;

public class Repository : IRepository
{
	SQLiteConnection _dbConnection;
	bool _isDisposed;

	public Repository(bool inMemory)
	{
		var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		// :memory: is not sufficient, see https://github.com/praeclarum/sqlite-net/issues/1077
		var fullPath = inMemory ? $"file:memdb_{Guid.NewGuid()}?mode=memory" : Path.Combine(folder, "fantasy-football.db3");
		Initialize(fullPath);
	}

	void Initialize(string fullPath)
	{
		_dbConnection = new SQLiteConnection(fullPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache | SQLiteOpenFlags.FullMutex);
		var modelTables = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass && t.Namespace == "FantasyFootball.Models" && !t.Attributes.HasFlag(TypeAttributes.NestedPrivate)).ToArray();
		_ = _dbConnection.CreateTables(CreateFlags.None, modelTables);
	}

	public List<T> GetAll<T>() where T : NamedUniqueId, new() => _dbConnection.GetAllWithChildren<T>(recursive: true);

	public Task<List<T>> GetAllAsync<T>() where T : NamedUniqueId, new() => Task.Run(() => _dbConnection.GetAllWithChildren<T>(recursive: true));

	public int Count<T>() where T : NamedUniqueId, new() => _dbConnection.Table<T>().Count();

	public T? Get<T>(int id) where T : NamedUniqueId, new()
	{
		try
		{
			return _dbConnection.GetWithChildren<T>(id, recursive: true);
		}
		catch (InvalidOperationException e)
		{
			Log.Error(e, $"Unable to find key {id} for type {typeof(T).Name}");
			return null;
		}
	}
	public void Save<T>(T item) where T : NamedUniqueId, new()
	{
		if (item.Id != 0)
		{
			_dbConnection.UpdateWithChildren(item);
		}
		else
		{
			_dbConnection.InsertWithChildren(item, recursive: true);
		}
	}

	public void Delete<T>(T item) where T : NamedUniqueId, new() => _dbConnection.Delete(item, recursive: true);

	protected virtual void Dispose(bool disposing)
	{
		if (!_isDisposed)
		{
			if (disposing)
			{
				_dbConnection.Dispose();
			}
			_isDisposed = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public void Reset()
	{
		var path = _dbConnection.DatabasePath;
		_dbConnection.Close();
		if (File.Exists(path)) { File.Delete(path); }
		Initialize(path);
	}
}
