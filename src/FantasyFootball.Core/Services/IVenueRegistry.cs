using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Lookup surface for the global venue catalog (~1700 stadiums bundled from
/// Wikidata SPARQL; see tools/fetch-venues.ps1). Resolves a venue by its slug
/// id — e.g. <c>"metlife-stadium"</c> — to the full Venue record. Returns
/// null for unknown ids so callers can render games whose <c>VenueId</c>
/// references a stadium not in the bundled set.
/// </summary>
public interface IVenueRegistry
{
	Venue? Get(string id);
}
