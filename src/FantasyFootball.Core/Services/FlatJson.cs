using System.Text.Json;
using System.Text.Json.Serialization;
using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> for the new flat model.
/// Defines the conventions used by competition-definition files,
/// runtime persistence, and the global Team / Venue registries:
///
/// <list type="bullet">
///   <item><b>camelCase</b> property names (so JSON matches conventional
///         data files rather than C# PascalCase).</item>
///   <item><b>String enums</b> via <see cref="JsonStringEnumConverter"/>
///         — <c>CompetitionType.WM</c> serializes as <c>"WM"</c>, not <c>0</c>.</item>
///   <item><b>Comments allowed</b> when reading — JSON definition files
///         use comments freely; serializer strips them.</item>
///   <item><b>Indented writes</b> — definition files are human-edited,
///         readability matters more than a few extra bytes.</item>
/// </list>
/// </summary>
public static class FlatJson
{
	public static readonly JsonSerializerOptions Options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		WriteIndented = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		// Drop null-valued fields on write — definition files are
		// schedule-only (Result, VenueId, resolved teamIds on KO games
		// are all null at this stage); writing them as explicit nulls
		// triples the file size and adds no information.
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters =
		{
			new JsonStringEnumConverter(),
			new UtcDateTimeConverter(),
		},
	};

	/// <summary>
	/// Compact (single-line, no indentation) variant of <see cref="Options"/>.
	/// Used by the runtime persistence path where bytes-on-disk matter
	/// (LocalStorage quota, eventual IndexedDB key size) and humans
	/// don't read the JSON. Indented <see cref="Options"/> stays the
	/// default for definition files and tests.
	/// </summary>
	public static readonly JsonSerializerOptions CompactOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		WriteIndented = false,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters =
		{
			new JsonStringEnumConverter(),
			new UtcDateTimeConverter(),
		},
	};

	/// <summary>
	/// Writes DateTime values as ISO 8601 with an explicit <c>Z</c> UTC
	/// marker (<c>2026-06-11T21:00:00Z</c>) regardless of the value's
	/// <see cref="DateTimeKind"/>. Without this, System.Text.Json emits
	/// timezone-less strings for Unspecified-kind values and dates round-trip
	/// as ambiguous local-or-UTC blobs — readers in other timezones get
	/// surprised.
	/// </summary>
	sealed class UtcDateTimeConverter : JsonConverter<DateTime>
	{
		public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			// Default reader handles Z, +00:00, and unmarked equally — keep it.
			return reader.GetDateTime();
		}

		public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
		{
			var asUtc = value.Kind == DateTimeKind.Utc
				? value
				: DateTime.SpecifyKind(value, DateTimeKind.Utc);
			writer.WriteStringValue(asUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"));
		}
	}

	/// <summary>
	/// Serialize a Competition to its on-disk JSON shape. The C# model
	/// holds groups as <c>string[][]</c> (positional, indexed by letter − 'A')
	/// but the JSON file shape is a dict <c>{ "A": [...], "B": [...] }</c> —
	/// this method does the array → dict reshape. Used by the definition
	/// generator and by the round-trip test.
	/// </summary>
	public static string Serialize(Competition c, bool compact = false)
	{
		var dto = new CompetitionDto(
			Id: c.DefinitionId,
			Title: c.Title,
			Type: c.Type,
			Year: c.Year,
			FormatId: c.FormatId,
			EloSetName: c.EloSetName,
			SimulationStart: c.SimulationStart,
			SimulationFinished: c.SimulationFinished,
			Stages: c.Stages,
			Rounds: c.Rounds,
			Groups: c.IsLeague() ? null : ToGroupsDict(c.GroupAssignments),
			Teams: c.IsLeague() ? c.Teams : null,
			QualificationSlots: c.QualificationSlots.Length == 0 ? null : c.QualificationSlots,
			Games: c.Games);
		return JsonSerializer.Serialize(dto, compact ? CompactOptions : Options);
	}

	/// <summary>On-disk shape of a Competition — drives both writes here and the read path in <see cref="CompetitionDefinitionLoader"/>.</summary>
	internal sealed record CompetitionDto(
		string Id,
		string Title,
		CompetitionType Type,
		int Year,
		string FormatId,
		string? EloSetName,
		DateTime? SimulationStart,
		DateTime? SimulationFinished,
		Stage[] Stages,
		Round[] Rounds,
		Dictionary<string, string[]>? Groups,
		string[]? Teams,
		QualificationSlot[]? QualificationSlots,
		Game[] Games);

	static Dictionary<string, string[]> ToGroupsDict(string[][] arr)
	{
		var dict = new Dictionary<string, string[]>(arr.Length);
		for (int i = 0; i < arr.Length; i++)
		{
			dict[((char)('A' + i)).ToString()] = arr[i];
		}
		return dict;
	}
}
