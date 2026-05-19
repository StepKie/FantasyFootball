using System.Text.Json;
using System.Text.Json.Serialization;

namespace FantasyFootball.Services;

/// <summary>
/// Shared JSON serializer options for the Competition aggregate.
/// Used by the LocalStorage repository (persistence) and the test suite (round-trip checks).
/// </summary>
public static class CompetitionSnapshot
{
	// STJ freezes options on first use; a single shared instance is the documented best practice.
	// MakeReadOnly() turns accidental late mutation into a clear InvalidOperationException instead of
	// silently affecting every other caller of these shared options.
	public static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

	static JsonSerializerOptions CreateJsonOptions()
	{
		var options = new JsonSerializerOptions
		{
			TypeInfoResolver = new IgnoreAttributeTypeInfoResolver(),
			ReferenceHandler = ReferenceHandler.Preserve,
		};
		options.MakeReadOnly();
		return options;
	}
}
