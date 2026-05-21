using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Provides access to the catalog of competition definitions a sim
/// can be seeded from. The default implementation, <see
/// cref="EmbeddedCompetitionDefinitionStore"/>, reads from JSON files
/// embedded in <c>FantasyFootball.Core</c>; alternative implementations
/// (filesystem, in-memory for tests, downloaded over HTTP later) plug
/// into the same surface.
///
/// Conceptually: <c>AvailableIds</c> is the list of canonical short
/// codes (<c>"wm-2022"</c>, <c>"em-2024"</c>, ...) the user can pick
/// from; <c>Load(id)</c> turns the chosen one into a fresh,
/// unsimulated <see cref="FlatCompetition"/>.
/// </summary>
public interface ICompetitionDefinitionStore
{
	IReadOnlyCollection<string> AvailableIds { get; }
	FlatCompetition Load(string definitionId);
}
