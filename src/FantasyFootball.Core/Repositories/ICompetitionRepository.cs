using FantasyFootball.Models;

namespace FantasyFootball.Repositories;

/// <summary>
/// Persistence surface for flat-model competitions. The Blazor WASM
/// host (web) and tests use this; the SQLite-backed
/// <see cref="IRepository"/> remains in the MAUI host (currently paused).
///
/// Implementations:
/// <list type="bullet">
///   <item><c>InMemoryCompetitionRepository</c> — tests + the working
///         set inside <c>BulkSimRunner</c> before user-visible persistence.</item>
///   <item><c>LocalStorageCompetitionRepository</c> — browser persistence
///         for the Blazor WASM host.</item>
///   <item><c>IndexedDbCompetitionRepository</c> (follow-up) —
///         higher-capacity browser storage for bulk sims that exceed
///         LocalStorage's 5–10 MB quota.</item>
/// </list>
/// </summary>
public interface ICompetitionRepository
{
	/// <summary>
	/// Persists a competition. If <c>Id == 0</c>, assigns a fresh ID
	/// and mutates the input to carry it; otherwise overwrites the
	/// existing entry under that ID. Returns the (now-assigned) ID.
	/// </summary>
	Task<int> SaveAsync(Competition competition);

	/// <summary>Loads a competition by repository ID, or null if no such ID.</summary>
	Task<Competition?> GetAsync(int id);

	/// <summary>Lists all persisted competitions. Order is implementation-defined.</summary>
	Task<IReadOnlyList<Competition>> GetAllAsync();

	/// <summary>Removes a competition by ID. No-op if the ID is unknown.</summary>
	Task DeleteAsync(int id);

	/// <summary>Number of persisted competitions.</summary>
	Task<int> CountAsync();

	/// <summary>Removes every persisted competition. Used by Settings → Reset.</summary>
	Task ResetAsync();
}
