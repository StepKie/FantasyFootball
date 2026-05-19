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
	public static readonly JsonSerializerOptions JsonOptions = new()
	{
		TypeInfoResolver = new IgnoreAttributeTypeInfoResolver(),
		ReferenceHandler = ReferenceHandler.Preserve,
	};
}
