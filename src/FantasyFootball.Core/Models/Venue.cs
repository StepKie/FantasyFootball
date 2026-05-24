namespace FantasyFootball.Models;

/// <summary>
/// A stadium / venue entry. Lives in a global registry (planned
/// <c>venues.json</c>, sibling to <c>teams.json</c>) and is referenced
/// by ID from <see cref="Game.VenueId"/>. Independent of any one
/// competition so the same Estadio Azteca can host games across
/// multiple tournaments without duplication.
///
/// Currently unpopulated — every <c>VenueId</c> on the committed
/// definition files is null. The type lives in the model so consumers
/// can be wired against it before the registry data exists.
/// </summary>
public sealed record class Venue(
	string Id,
	string Name,
	string City,
	string Country,
	int? Capacity);
