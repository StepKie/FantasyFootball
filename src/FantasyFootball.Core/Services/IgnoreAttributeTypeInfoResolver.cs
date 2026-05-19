using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FantasyFootball.Models;
using SQLite;

namespace FantasyFootball.Services;

/// <summary>
/// JSON type-info resolver that strips properties we don't want persisted to LocalStorage
/// or captured in in-memory Competition snapshots (e.g. undo).
///
/// Two filter rules:
///
/// 1. **Exact-type <see cref="IgnoreAttribute"/>**: computed / derived properties
///    (<c>Competition.CurrentStatus</c>, <c>Game.Winner</c>, <c>Team.IsNationalTeam</c>, …).
///    These are getter-only convenience helpers — serializing them would call the getter
///    and either NRE on partially-built graphs or duplicate state already represented
///    elsewhere.
///
/// 2. **Explicit back-pointer collections** (<see cref="BackPointerCollections"/>): the
///    inverse side of a <c>[ManyToOne]</c> relationship — e.g. <c>Confederation.Countries</c>
///    (inverse of <c>Country.Confederation</c>) and <c>Country.Clubs</c> (inverse of
///    <c>Team.Country</c>). Serializing these would walk every country / club into every
///    confederation, exploding the persisted graph.
///
/// Everything else stays — including forward-owning collections (<c>Competition.Stages</c>,
/// <c>Stage.Rounds</c>, <c>Round.RegularGames</c>, <c>Group.Teams</c>, …) so the Competition
/// graph round-trips intact. STJ's <see cref="System.Text.Json.Serialization.ReferenceHandler.Preserve"/>
/// resolves cycles created by the back-references (<c>Stage.Competition</c>, <c>Game.Round</c>)
/// via <c>$id</c> / <c>$ref</c>.
/// </summary>
public sealed class IgnoreAttributeTypeInfoResolver : DefaultJsonTypeInfoResolver
{
	static readonly HashSet<(Type DeclaringType, string PropertyName)> BackPointerCollections =
	[
		(typeof(Confederation), nameof(Confederation.Countries)),
		(typeof(Country), nameof(Country.Clubs)),
	];

	public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		var info = base.GetTypeInfo(type, options);

		var toRemove = new List<JsonPropertyInfo>();
		foreach (var property in info.Properties)
		{
			// AttributeProvider is the underlying PropertyInfo for reflection-emitted contracts.
			// Using it directly avoids a second type.GetProperty lookup (and its AmbiguousMatch /
			// IgnoreCase pitfalls) and gives us the CLR name regardless of any JsonNamingPolicy.
			if (property.AttributeProvider is not PropertyInfo propInfo) { continue; }

			var hasExactIgnore = propInfo
				.GetCustomAttributes(typeof(IgnoreAttribute), inherit: false)
				.Any(a => a.GetType() == typeof(IgnoreAttribute));

			var isBackPointer = BackPointerCollections.Contains((type, propInfo.Name));

			if (hasExactIgnore || isBackPointer) { toRemove.Add(property); }
		}

		foreach (var p in toRemove) { info.Properties.Remove(p); }

		return info;
	}
}
