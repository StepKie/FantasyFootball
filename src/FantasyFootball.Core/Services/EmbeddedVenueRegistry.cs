using System.Reflection;
using System.Text.Json;
using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Loads the bundled <c>venues.json</c> (embedded resource at
/// <c>FantasyFootball.Resources.Data.venues.json</c>) once and serves
/// lookups from an in-memory dictionary. Same loading shape as
/// <see cref="EmbeddedCompetitionDefinitionStore"/> — single-pass deserialize
/// at construction, no caching layer or invalidation.
/// </summary>
public sealed class EmbeddedVenueRegistry : IVenueRegistry
{
	const string VenuesResource = "FantasyFootball.Resources.Data.venues.json";

	readonly Dictionary<string, Venue> _byId;

	public EmbeddedVenueRegistry()
	{
		using var stream = typeof(EmbeddedVenueRegistry).Assembly.GetManifestResourceStream(VenuesResource)
			?? throw new FileNotFoundException($"{VenuesResource} not found in embedded resources");
		var venues = JsonSerializer.Deserialize<Venue[]>(stream, FlatJson.Options)
			?? throw new FormatException("venues.json deserialized to null");
		_byId = venues.ToDictionary(v => v.Id, v => v);
	}

	public Venue? Get(string id) => _byId.TryGetValue(id, out var v) ? v : null;
}
