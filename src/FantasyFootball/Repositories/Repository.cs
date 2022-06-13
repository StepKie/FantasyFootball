namespace FantasyFootball.Repositories;

public class Repository : IRepository
{
	SQLiteConnection _dbConnection;
	bool _isDisposed;

	public Repository(bool inMemory)
	{
		var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		// :memory: is a hardcoded string that will create a in-memory db
		var fullPath = inMemory ? ":memory:" : Path.Combine(folder, "fantasy-football.db3");
		Initialize(fullPath);
	}

	void Initialize(string fullPath)
	{
		_dbConnection = new SQLiteConnection(fullPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache | SQLiteOpenFlags.FullMutex);
		_ = _dbConnection.CreateTables(CreateFlags.None,
			typeof(NamedUniqueId),
			typeof(Group),
			typeof(Team),
			typeof(TeamGroupAssignment),
			typeof(Competition),
			typeof(Stage),
			typeof(Round),
			typeof(Game),
			typeof(Country),
			typeof(Confederation),
			typeof(TeamRecord)
		);
	}

	[Time]
	public IList<T> GetAll<T>() where T : NamedUniqueId, new() => _dbConnection.GetAllWithChildren<T>(recursive: true);

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

	[Time]
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
