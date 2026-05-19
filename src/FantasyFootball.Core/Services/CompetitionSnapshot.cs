using System.Text.Json;
using System.Text.Json.Serialization;
using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Shared JSON snapshot helpers for the Competition aggregate.
/// Used by LocalStorageRepository (persistence) and the undo stack (in-memory rewind).
/// Both must use identical options so a string captured by one path round-trips through the other.
/// </summary>
public static class CompetitionSnapshot
{
	// STJ freezes options on first use; a single shared instance is the documented best practice.
	public static readonly JsonSerializerOptions JsonOptions = new()
	{
		TypeInfoResolver = new IgnoreAttributeTypeInfoResolver(),
		ReferenceHandler = ReferenceHandler.Preserve,
	};

	public static string Serialize(Competition competition) => JsonSerializer.Serialize(competition, JsonOptions);

	public static Competition? Deserialize(string json) => JsonSerializer.Deserialize<Competition>(json, JsonOptions);
}
