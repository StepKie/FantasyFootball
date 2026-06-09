using System.Text.Json;
using Blazored.LocalStorage;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using Serilog;

namespace FantasyFootball.Web.Services;

/// <summary>
/// IRepository implementation that persists each aggregate-root type as a JSON list
/// under its own browser LocalStorage key. Aggregate roots: Team, Confederation,
/// EloSet — TeamDetailViewModel writes Elo edits into the active EloSet via this
/// repo. The flat competition model has its own repository
/// (<see cref="IndexedDbCompetitionRepository"/>).
/// </summary>
public sealed class LocalStorageRepository : IRepository
{
  const string KeyPrefix = "fantasy-football:";

  static readonly HashSet<Type> AggregateRoots =
  [
    typeof(Team),
    typeof(Confederation),
    typeof(EloSet),
  ];

  static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

  readonly ISyncLocalStorageService _localStorage;
  readonly Dictionary<Type, Dictionary<int, NamedUniqueId>> _buckets = [];

  public LocalStorageRepository(ISyncLocalStorageService localStorage)
  {
    _localStorage = localStorage;
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
    ThrowIfNotAggregateRoot(typeof(T));
    var bucket = LoadBucket<T>();
    if (item.Id == 0)
    {
      item.Id = bucket.Keys.DefaultIfEmpty(0).Max() + 1;
    }
    bucket[item.Id] = item;
    PersistBucket<T>(bucket);
  }

  public void SaveAll<T>(IEnumerable<T> items) where T : NamedUniqueId, new()
  {
    ThrowIfNotAggregateRoot(typeof(T));
    var bucket = LoadBucket<T>();
    var nextId = bucket.Keys.DefaultIfEmpty(0).Max() + 1;
    foreach (var item in items)
    {
      if (item.Id == 0) { item.Id = nextId++; }
      bucket[item.Id] = item;
    }
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
    List<T> items;
    try
    {
      items = string.IsNullOrEmpty(raw)
        ? []
        : JsonSerializer.Deserialize<List<T>>(raw, JsonOptions) ?? [];
    }
    catch (JsonException ex)
    {
      // Quarantine the corrupt blob so the page renders; Settings → Reset clears both.
      try
      {
        var bad = KeyFor<T>() + ":corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        _localStorage.SetItemAsString(bad, raw!);
        _localStorage.RemoveItem(KeyFor<T>());
        Log.Warning("[LocalStorageRepository] Failed to deserialize {Bucket} bucket; corrupt blob moved to '{Quarantine}'. Length={Length}. Error: {Error}", typeof(T).Name, bad, raw!.Length, ex.Message);
      }
      catch (Exception quarantineEx)
      {
        Log.Warning("[LocalStorageRepository] Quarantine write for {Bucket} failed: {Error}. Removing corrupt key and continuing with empty bucket.", typeof(T).Name, quarantineEx.Message);
        try { _localStorage.RemoveItem(KeyFor<T>()); }
        catch (Exception removeEx) { Log.Warning("[LocalStorageRepository] Could not remove corrupt {Bucket} key: {Error}. Corrupt data may persist on next load.", typeof(T).Name, removeEx.Message); }
      }
      items = [];
    }
    var bucket = items.ToDictionary(x => x.Id, x => (NamedUniqueId)x);
    _buckets[typeof(T)] = bucket;
    return bucket;
  }

  void PersistBucket<T>(Dictionary<int, NamedUniqueId> bucket) where T : NamedUniqueId, new()
  {
    var items = bucket.Values.Cast<T>().ToList();
    var raw = JsonSerializer.Serialize(items, JsonOptions);
    _localStorage.SetItemAsString(KeyFor<T>(), raw);
  }

  static string KeyFor<T>() => KeyPrefix + typeof(T).Name;

  static void ThrowIfNotAggregateRoot(Type t)
  {
    if (!AggregateRoots.Contains(t))
    {
      throw new NotSupportedException(
        $"{t.Name} is not an aggregate root in LocalStorageRepository. Aggregate roots: {string.Join(", ", AggregateRoots.Select(x => x.Name))}.");
    }
  }
}
