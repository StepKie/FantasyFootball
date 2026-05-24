using System.Reflection;
using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Reads competition definitions from JSON files embedded in
/// <c>FantasyFootball.Core</c>'s <c>Definitions/</c> folder.
/// Manifest names match <c>FantasyFootball.Definitions.{id}.json</c>;
/// the prefix comes from the project's <c>RootNamespace</c>, which is
/// pinned to <c>FantasyFootball</c> in the csproj (not
/// <c>FantasyFootball.Core</c>).
/// </summary>
public sealed class EmbeddedCompetitionDefinitionStore : ICompetitionDefinitionStore
{
	const string ResourcePrefix = "FantasyFootball.Definitions.";
	const string ResourceSuffix = ".json";

	static readonly Assembly DefinitionAssembly = typeof(EmbeddedCompetitionDefinitionStore).Assembly;
	static readonly IReadOnlyCollection<string> CachedIds = DiscoverIds();

	public IReadOnlyCollection<string> AvailableIds => CachedIds;

	public Competition Load(string definitionId)
	{
		var resourceName = $"{ResourcePrefix}{definitionId}{ResourceSuffix}";
		using var stream = DefinitionAssembly.GetManifestResourceStream(resourceName)
			?? throw new FileNotFoundException(
				$"No embedded competition definition for '{definitionId}'. " +
				$"Available: {string.Join(", ", CachedIds)}.");
		return CompetitionDefinitionLoader.LoadFromStream(stream);
	}

	static IReadOnlyCollection<string> DiscoverIds() => DefinitionAssembly
		.GetManifestResourceNames()
		.Where(n => n.StartsWith(ResourcePrefix) && n.EndsWith(ResourceSuffix))
		.Select(n => n[ResourcePrefix.Length..^ResourceSuffix.Length])
		.OrderBy(id => id)
		.ToList();
}
