using System.Text.Json;
using System.Text.Json.Serialization;
using Blazored.LocalStorage;
using FantasyFootball.Models;
using FantasyFootball.Repositories;

namespace FantasyFootball.Web.Services;

/// <summary>
/// IRepository implementation that persists each aggregate-root type as a JSON list
/// under its own browser LocalStorage key. Non-root nested types (Stage, Round, Game,
/// Qualifier, …) live inside their containing Competition's JSON; saving a Game walks
/// up to the Competition and re-persists the aggregate.
///
/// Differs from the SQLite Repository in two intentional ways:
///   1. No background table for nested types — GetAll&lt;Round&gt; / GetAll&lt;Game&gt; will throw.
///      Callers should navigate through Competition.Stages / Rounds / AllGames instead.
///   2. Save is synchronous: in-memory dictionary mutation followed by a synchronous
///      JSON serialize + SetItem call. Acceptable in WASM because LocalStorage I/O is
///      already main-thread; under our data volume (low hundreds of KB) latency is sub-ms.
/// </summary>
public sealed class LocalStorageRepository : IRepository
{
  const string KeyPrefix = "fantasy-football:";

  static readonly HashSet<Type> AggregateRoots =
  [
    typeof(Competition),
    typeof(Team),
    typeof(Confederation),
    typeof(Country),
  ];

  readonly ISyncLocalStorageService _localStorage;
  readonly JsonSerializerOptions _jsonOptions;
  readonly Dictionary<Type, Dictionary<int, NamedUniqueId>> _buckets = [];

  public LocalStorageRepository(ISyncLocalStorageService localStorage)
  {
    _localStorage = localStorage;
    _jsonOptions = new JsonSerializerOptions
    {
      TypeInfoResolver = new IgnoreAttributeTypeInfoResolver(),
      ReferenceHandler = ReferenceHandler.Preserve,
    };
  }

  public List<T> GetAll<T>() where T : NamedUniqueId, new()
  {
    ThrowIfNotAggregateRoot(typeof(T));
    return LoadBucket<T>().Values.Cast<T>().ToList();
  }

  public Task<List<T>> GetAllAsync<T>() where T : NamedUniqueId, new() => Task.FromResult(GetAll<T>());

  public T? Get<T>(int id) where T : NamedUniqueId, new()
  {
    ThrowIfNotAggregateRoot(typeof(T));
    return LoadBucket<T>().TryGetValue(id, out var item) ? (T)item : null;
  }

  public int Count<T>() where T : NamedUniqueId, new()
  {
    ThrowIfNotAggregateRoot(typeof(T));
    return LoadBucket<T>().Count;
  }

  public void Save<T>(T item) where T : NamedUniqueId, new()
  {
    // Non-root types defer to the containing Competition. CompetitionSimulator
    // calls Repo.Save(game) mid-simulation; we treat that as a Competition update.
    if (item is Game game)
    {
      var competition = game.Round?.Stage?.Competition
        ?? throw new InvalidOperationException(
          $"Cannot save Game {game.Id}: missing Round.Stage.Competition back-reference.");
      Save(competition);
      return;
    }

    ThrowIfNotAggregateRoot(typeof(T));
    var bucket = LoadBucket<T>();
    if (item.Id == 0)
    {
      item.Id = bucket.Keys.DefaultIfEmpty(0).Max() + 1;
    }
    bucket[item.Id] = item;
    PersistBucket<T>(bucket);
  }

  public void Delete<T>(T item) where T : NamedUniqueId, new()
  {
    ThrowIfNotAggregateRoot(typeof(T));
    var bucket = LoadBucket<T>();
    if (bucket.Remove(item.Id))
    {
      PersistBucket<T>(bucket);
    }
  }

  public void Reset()
  {
    var ourKeys = _localStorage.Keys().Where(k => k.StartsWith(KeyPrefix)).ToList();
    foreach (var key in ourKeys)
    {
      _localStorage.RemoveItem(key);
    }
    _buckets.Clear();
  }

  public void Dispose() { }

  Dictionary<int, NamedUniqueId> LoadBucket<T>() where T : NamedUniqueId, new()
  {
    if (_buckets.TryGetValue(typeof(T), out var cached)) return cached;

    var raw = _localStorage.GetItemAsString(KeyFor<T>());
    var items = string.IsNullOrEmpty(raw)
      ? []
      : JsonSerializer.Deserialize<List<T>>(raw, _jsonOptions) ?? [];
    var bucket = items.ToDictionary(x => x.Id, x => (NamedUniqueId)x);
    _buckets[typeof(T)] = bucket;
    return bucket;
  }

  void PersistBucket<T>(Dictionary<int, NamedUniqueId> bucket) where T : NamedUniqueId, new()
  {
    var items = bucket.Values.Cast<T>().ToList();
    var raw = JsonSerializer.Serialize(items, _jsonOptions);
    _localStorage.SetItemAsString(KeyFor<T>(), raw);
  }

  static string KeyFor<T>() => KeyPrefix + typeof(T).Name;

  static void ThrowIfNotAggregateRoot(Type t)
  {
    if (!AggregateRoots.Contains(t))
    {
      throw new NotSupportedException(
        $"{t.Name} is not an aggregate root in LocalStorageRepository. " +
        $"Access nested types through Competition (e.g. competition.Rounds[i].AllGames[j]).");
    }
  }
}
