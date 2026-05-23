using FantasyFootball.Models;

namespace FantasyFootball.Repositories;

/// <summary>
/// Persistence surface for the new flat-model competitions, used by
/// the bulk-simulation flow and by the single-competition UI once it
/// cuts over. Independent of the old graph-shaped <see
/// cref="IRepository"/> — they live side by side during the migration
/// and the old one disappears in the cleanup PR.
///
/// Implementations:
/// <list type="bullet">
///   <item><c>InMemoryFlatCompetitionRepository</c> — for tests, and
///         as the working set inside <c>BulkSimRunner</c> before any
///         user-visible persistence.</item>
///   <item><c>LocalStorageFlatCompetitionRepository</c> (UI cutover PR)
///         — browser persistence for the Blazor WASM host.</item>
///   <item><c>IndexedDbFlatCompetitionRepository</c> (follow-up PR) —
///         higher-capacity browser storage for bulk sims that exceed
///         LocalStorage's 5–10 MB quota.</item>
/// </list>
/// </summary>
public interface IFlatCompetitionRepository
{
	/// <summary>
	/// Persists a competition. If <c>Id == 0</c>, assigns a fresh ID
	/// and mutates the input to carry it; otherwise overwrites the
	/// existing entry under that ID. Returns the (now-assigned) ID.
	/// </summary>
	Task<int> SaveAsync(FlatCompetition competition);

	/// <summary>Loads a competition by repository ID, or null if no such ID.</summary>
	Task<FlatCompetition?> GetAsync(int id);

	/// <summary>Lists all persisted competitions. Order is implementation-defined.</summary>
	Task<IReadOnlyList<FlatCompetition>> GetAllAsync();

	/// <summary>Removes a competition by ID. No-op if the ID is unknown.</summary>
	Task DeleteAsync(int id);

	/// <summary>Number of persisted competitions.</summary>
	Task<int> CountAsync();

	/// <summary>Removes every persisted competition. Used by Settings → Reset.</summary>
	Task ResetAsync();
}
