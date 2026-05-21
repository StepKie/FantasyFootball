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
		// Old model is the only thing SQLite knows how to map — gated by
		// presence of [Table]. New-model types (Flat*, CompetitionSpec, …)
		// live in the same namespace but skip persistence here; the JSON-
		// driven flat repo handles those. Removed entirely in the cleanup
		// PR alongside this file.
		var modelTables = Assembly.GetExecutingAssembly().GetTypes()
			.Where(t => t.IsClass
				&& t.Namespace == "FantasyFootball.Models"
				&& !t.Attributes.HasFlag(TypeAttributes.NestedPrivate)
				&& t.GetCustomAttribute<SQLite.TableAttribute>() is not null)
			.ToArray();
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
		// Targeted single-row update for Games: VM per-game flows (SimulateGame, Undo,
		// Redo) call Save(game) and shouldn't pay the cost of cascading the whole
		// Competition graph. UpdateWithChildren on a Game would also trigger the
		// multi-OneToOne FK stomp on its qualifiers — single Update sidesteps both.
		// New Games (Id == 0) intentionally fall through to the cascade path below;
		// the factory always creates them under a Competition, so a bare-Game insert
		// isn't a real flow.
		if (item is FantasyFootball.Models.Game game && game.Id != 0)
		{
			_dbConnection.Update(game);
			return;
		}
		if (item.Id != 0)
		{
			// Same multi-OneToOne-same-type FK stomping happens on UpdateWithChildren
			// for KoGames whose qualifiers were stomped on the original insert OR
			// whose qualifiers were added/changed in memory after the insert.
			// Snapshot qualifier IDs from the in-memory model and restore via SQL.
			var snapshot = SnapshotKoGameQualifierIds(item);
			_dbConnection.UpdateWithChildren(item);
			RestoreKoGameQualifierForeignKeys(item, snapshot);
			// UpdateWithChildren only updates the top-level entity and its relations,
			// not the actual deep-child rows. After CompetitionSimulator mutates game
			// scores in memory, we have to persist each Game record explicitly.
			UpdateAllGames(item);
		}
		else
		{
			// Workaround for sqlite-net-extensions: when a single entity (KoGame)
			// has multiple OneToOne navigation properties pointing to the same
			// child type (HomeGroupQualifier + AwayGroupQualifier both point to
			// GroupQualifier), the cascade insert stomps the first FK AND the
			// first nav property reference with the second child's Id/ref. Both
			// HomeGroupQualifierId and AwayGroupQualifierId end up referencing
			// the same row, and ko.HomeGroupQualifier == ko.AwayGroupQualifier in
			// memory. Symptom: KoGame.HomeTeam == KoGame.AwayTeam after reload.
			//
			// Repair sequence:
			//   1. PreInsert qualifiers individually so each has a distinct Id.
			//   2. Snapshot the correct (HomeId, AwayId) per KoGame *before* the
			//      cascade, since the cascade will mutate the in-memory refs.
			//   3. Cascade-insert the rest of the graph.
			//   4. Post-update each KoGame row via SQL with the snapshotted FKs.
			PreInsertKoGameQualifiers(item);
			var snapshot = SnapshotKoGameQualifierIds(item);
			_dbConnection.InsertWithChildren(item, recursive: true);
			RestoreKoGameQualifierForeignKeys(item, snapshot);
		}
	}

	void PreInsertKoGameQualifiers<T>(T item) where T : NamedUniqueId
	{
		if (item is not FantasyFootball.Models.Competition comp) { return; }
		foreach (var stage in comp.Stages)
		foreach (var round in stage.Rounds)
		foreach (var ko in round.KoGames)
		{
			PreInsertOne(ko.HomeGroupQualifier);
			PreInsertOne(ko.AwayGroupQualifier);
			PreInsertOne(ko.HomeGameQualifier);
			PreInsertOne(ko.AwayGameQualifier);
		}
	}

	void PreInsertOne(NamedUniqueId? qualifier)
	{
		if (qualifier is null || qualifier.Id != 0) { return; }
		_dbConnection.Insert(qualifier);
	}

	// Keyed by KoGame reference (Id == 0 at snapshot time; we use the in-memory ref).
	Dictionary<FantasyFootball.Models.KoGame, (int HomeGroup, int AwayGroup, int HomeGame, int AwayGame)> SnapshotKoGameQualifierIds<T>(T item) where T : NamedUniqueId
	{
		var snapshot = new Dictionary<FantasyFootball.Models.KoGame, (int, int, int, int)>(ReferenceEqualityComparer.Instance);
		if (item is FantasyFootball.Models.Competition comp)
		{
			foreach (var stage in comp.Stages)
			foreach (var round in stage.Rounds)
			foreach (var ko in round.KoGames)
			{
				snapshot[ko] = (
					ko.HomeGroupQualifier?.Id ?? 0,
					ko.AwayGroupQualifier?.Id ?? 0,
					ko.HomeGameQualifier?.Id ?? 0,
					ko.AwayGameQualifier?.Id ?? 0);
			}
		}
		return snapshot;
	}

	void UpdateAllGames<T>(T item) where T : NamedUniqueId
	{
		if (item is not FantasyFootball.Models.Competition comp) { return; }
		foreach (var stage in comp.Stages)
		foreach (var round in stage.Rounds)
		{
			foreach (var game in round.RegularGames) { _dbConnection.Update(game); }
			foreach (var ko in round.KoGames) { _dbConnection.Update(ko); }
		}
	}

	void RestoreKoGameQualifierForeignKeys<T>(T item, Dictionary<FantasyFootball.Models.KoGame, (int HomeGroup, int AwayGroup, int HomeGame, int AwayGame)> snapshot) where T : NamedUniqueId
	{
		if (item is not FantasyFootball.Models.Competition comp) { return; }
		foreach (var stage in comp.Stages)
		foreach (var round in stage.Rounds)
		foreach (var ko in round.KoGames)
		{
			if (!snapshot.TryGetValue(ko, out var ids)) { continue; }
			_dbConnection.Execute(
				"UPDATE KoGame SET HomeGroupQualifierId = ?, AwayGroupQualifierId = ?, HomeGameQualifierId = ?, AwayGameQualifierId = ? WHERE Id = ?",
				ids.HomeGroup, ids.AwayGroup, ids.HomeGame, ids.AwayGame, ko.Id);
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
