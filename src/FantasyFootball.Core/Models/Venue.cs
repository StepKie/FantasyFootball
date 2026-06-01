namespace FantasyFootball.Models;

/// <summary>
/// A stadium / venue entry. Lives in a global registry
/// (<c>Resources/Data/venues.json</c>, embedded as a resource) and is
/// referenced by ID from <see cref="Game.VenueId"/>. Independent of any
/// one competition so the same Estadio Azteca can host games across multiple
/// tournaments without duplication.
/// </summary>
public sealed record class Venue
{
	public required string Id { get; init; }
	public required string Name { get; init; }
	public string? City { get; init; }
	/// <summary>ISO 3166-1 alpha-3 code (matches <c>Country.Code3</c>). Nullable when the source has no country claim for the venue.</summary>
	public string? CountryCode { get; init; }
	public required int Capacity { get; init; }
	/// <summary>Slugs of clubs that play their home matches here. Populated for club home grounds, empty for neutral / tournament-host venues.</summary>
	public string[] TenantClubs { get; init; } = [];
}
