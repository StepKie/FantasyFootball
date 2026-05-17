using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SQLite;

namespace FantasyFootball.Web.Services;

/// <summary>
/// JSON type-info resolver that excludes properties marked with sqlite-net-pcl's
/// <see cref="IgnoreAttribute"/>. The core models use [Ignore] to mark computed /
/// derived properties (Winner, IsFinished, GamesByDate, etc.) that shouldn't be
/// persisted. The default System.Text.Json resolver doesn't know about [Ignore]
/// and would serialize them — both bloating the payload and re-walking the graph
/// in ways that surface cycles.
/// </summary>
public sealed class IgnoreAttributeTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var info = base.GetTypeInfo(type, options);
        foreach (var property in info.Properties)
        {
            if (property.AttributeProvider?.GetCustomAttributes(typeof(IgnoreAttribute), inherit: true).Length > 0)
            {
                property.ShouldSerialize = static (_, _) => false;
                property.Get = null;
                property.Set = null;
            }
        }
        return info;
    }
}
