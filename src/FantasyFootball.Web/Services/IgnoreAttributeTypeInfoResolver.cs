using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace FantasyFootball.Web.Services;

/// <summary>
/// JSON type-info resolver that strips properties we don't want persisted to LocalStorage.
///
/// SQLite-Net-Extensions' relationship attributes (<c>[ManyToOne]</c>, <c>[OneToOne]</c>,
/// <c>[OneToMany]</c>, <c>[ManyToMany]</c>) all derive from <c>RelationshipAttribute</c>
/// which derives from sqlite-net's <see cref="IgnoreAttribute"/> — so a naive
/// "filter by IgnoreAttribute inheritance" sweep also wipes navigation properties out
/// of the JSON. For our model that means <c>Team.Country</c> and <c>Country.Confederation</c>
/// get dropped, leaving deserialized Teams with <c>Country = null</c> (and no flags to
/// render off Code2).
///
/// What we actually want filtered:
///   - Exact-type <see cref="IgnoreAttribute"/>: computed / derived properties like
///     <c>Game.Winner</c>, <c>Competition.GamesByDate</c>, <c>Team.IsNationalTeam</c>.
///   - <see cref="OneToManyAttribute"/> / <see cref="ManyToManyAttribute"/>: back-pointer
///     collections (<c>Confederation.Countries</c>, <c>Country.Clubs</c>) that would
///     explode the persisted graph if walked.
///
/// What stays:
///   - <see cref="ManyToOneAttribute"/> / <see cref="OneToOneAttribute"/>: single forward
///     navigation refs. STJ's <c>ReferenceHandler.Preserve</c> resolves the small cycles
///     these create (Team → Country → Team via NationalTeam) via $id / $ref.
///   - <see cref="ForeignKeyAttribute"/>: it's just an <c>int</c>, no inheritance from Ignore.
/// </summary>
public sealed class IgnoreAttributeTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var info = base.GetTypeInfo(type, options);
        foreach (var property in info.Properties)
        {
            var attrs = property.AttributeProvider?.GetCustomAttributes(typeof(IgnoreAttribute), inherit: true);
            if (attrs is null || attrs.Length == 0) { continue; }

            var shouldFilter = attrs.Any(a =>
                a.GetType() == typeof(IgnoreAttribute) ||
                a is OneToManyAttribute ||
                a is ManyToManyAttribute);

            if (shouldFilter)
            {
                property.ShouldSerialize = static (_, _) => false;
            }
        }
        return info;
    }
}
