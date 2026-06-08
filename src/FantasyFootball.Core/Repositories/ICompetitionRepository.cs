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
///   <item><c>IndexedDbCompetitionRepository</c> — browser persistence
///         for the Blazor WASM host. Each competition is one record in an
///         IndexedDB object store, escaping LocalStorage's ~5 MB cap.</item>
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

	/// <summary>
	/// Lists all persisted competitions, fully materialized. Heavy — deserializes
	/// every game of every competition. Use <see cref="GetAllSummariesAsync"/> for
	/// the list view; reserve this for the overall-standings aggregate, which needs
	/// per-game results. Order is implementation-defined.
	/// </summary>
	Task<IReadOnlyList<Competition>> GetAllAsync();

	/// <summary>
	/// Lists a lightweight <see cref="CompetitionSummary"/> for every persisted
	/// competition — enough to render the list without deserializing the games.
	/// Order is implementation-defined.
	/// </summary>
	Task<IReadOnlyList<CompetitionSummary>> GetAllSummariesAsync();

	/// <summary>Removes a competition by ID. No-op if the ID is unknown.</summary>
	Task DeleteAsync(int id);

	/// <summary>Number of persisted competitions.</summary>
	Task<int> CountAsync();

	/// <summary>Removes every persisted competition. Used by Settings → Reset.</summary>
	Task ResetAsync();
}
