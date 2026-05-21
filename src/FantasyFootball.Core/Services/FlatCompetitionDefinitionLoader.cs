using System.Text.Json;
using FantasyFootball.Data;
using FantasyFootball.Models;

namespace FantasyFootball.Services;

/// <summary>
/// Reads a competition-definition JSON document and produces a fresh
/// (unsimulated) <see cref="FlatCompetition"/> ready for sim — Stages,
/// Rounds, GroupAssignments, and Games all populated; all Results null.
///
/// The JSON shape is loose-but-typed: top-level fields (id, title, type,
/// year, formatId, stages, rounds, games) deserialize directly via
/// System.Text.Json with the conventions in <see cref="FlatJson"/>.
/// Two intermediate shapes need bridging:
///
/// <list type="bullet">
///   <item><c>groups</c>: a dict of <c>letter → string[]</c> in JSON for
///         readability. Converted into an ordered <c>string[][]</c>
///         indexed by (letter − 'A') on the C# side.</item>
///   <item><c>games[*].kind</c>: <c>"group"</c> / <c>"ko"</c>
///         discriminator handled via the
///         <see cref="System.Text.Json.Serialization.JsonPolymorphicAttribute"/>
///         already on <see cref="FlatGame"/>.</item>
/// </list>
///
/// Loaders are stateless — no caching, no I/O. Caching is the caller's
/// responsibility (a definition store will typically cache per id).
/// </summary>
public static class FlatCompetitionDefinitionLoader
{
	public static FlatCompetition Load(string json)
	{
		var raw = JsonSerializer.Deserialize<RawDefinition>(json, FlatJson.Options)
			?? throw new FormatException("Competition definition JSON deserialized to null.");
		return MaterializeCompetition(raw);
	}

	public static FlatCompetition LoadFromStream(Stream stream)
	{
		var raw = JsonSerializer.Deserialize<RawDefinition>(stream, FlatJson.Options)
			?? throw new FormatException("Competition definition JSON deserialized to null.");
		return MaterializeCompetition(raw);
	}

	static FlatCompetition MaterializeCompetition(RawDefinition raw)
	{
		// Required-property validation: System.Text.Json's `required` support
		// catches missing fields at deserialize time, but the inputs may still
		// be empty arrays / dicts. Validate at the shape boundary.
		if (raw.Stages is null || raw.Stages.Length == 0)
		{
			throw new FormatException($"Competition '{raw.Id}' has no stages.");
		}
		if (raw.Rounds is null || raw.Rounds.Length == 0)
		{
			throw new FormatException($"Competition '{raw.Id}' has no rounds.");
		}
		if (raw.Games is null || raw.Games.Length == 0)
		{
			throw new FormatException($"Competition '{raw.Id}' has no games.");
		}

		// Cross-reference checks: every game's roundId must exist in rounds;
		// every round's stageId must exist in stages.
		var stageIds = raw.Stages.Select(s => s.Id).ToHashSet();
		foreach (var r in raw.Rounds)
		{
			if (!stageIds.Contains(r.StageId))
			{
				throw new FormatException($"Round '{r.Id}' references unknown stage '{r.StageId}' in competition '{raw.Id}'.");
			}
		}
		var roundIds = raw.Rounds.Select(r => r.Id).ToHashSet();
		foreach (var g in raw.Games)
		{
			if (!roundIds.Contains(g.RoundId))
			{
				throw new FormatException($"Game {g.Id} references unknown round '{g.RoundId}' in competition '{raw.Id}'.");
			}
		}

		// Reshape groups dict → array indexed by (letter - 'A'). Skip if
		// the format is knockout-only (no groups in the JSON).
		var groupAssignments = ConvertGroupsToArray(raw.Groups, raw.Id);

		return new FlatCompetition
		{
			Id = 0,                               // Repository assigns Id at Save; the definition itself isn't persisted as a Competition row.
			Title = raw.Title,
			Type = raw.Type,
			Year = raw.Year,
			DefinitionId = raw.Id,
			FormatId = raw.FormatId,
			SimulationStart = raw.SimulationStart,
			SimulationFinished = raw.SimulationFinished,
			GroupAssignments = groupAssignments,
			Stages = raw.Stages,
			Rounds = raw.Rounds,
			Games = raw.Games,
		};
	}

	static string[][] ConvertGroupsToArray(Dictionary<string, string[]>? groups, string definitionId)
	{
		if (groups is null || groups.Count == 0) { return []; }

		// Determine the alphabet span: highest letter used.
		var maxLetterIndex = groups.Keys.Max(k =>
		{
			if (k.Length != 1 || !char.IsAsciiLetterUpper(k[0]))
			{
				throw new FormatException($"Group key '{k}' in competition '{definitionId}' must be a single uppercase letter A-Z.");
			}
			return k[0] - 'A';
		});

		var arr = new string[maxLetterIndex + 1][];
		for (int i = 0; i < arr.Length; i++)
		{
			var letter = ((char)('A' + i)).ToString();
			if (!groups.TryGetValue(letter, out var teams))
			{
				throw new FormatException($"Group letter '{letter}' missing in competition '{definitionId}' — gaps in the alphabet aren't supported.");
			}
			arr[i] = teams;
		}
		return arr;
	}

	/// <summary>
	/// Raw JSON shape — close to the on-disk format. Bridged to
	/// FlatCompetition by <see cref="MaterializeCompetition"/>.
	/// </summary>
	sealed class RawDefinition
	{
		public required string Id { get; init; }
		public required string Title { get; init; }
		public required CompetitionType Type { get; init; }
		public required int Year { get; init; }
		public required string FormatId { get; init; }
		public DateTime? SimulationStart { get; init; }
		public DateTime? SimulationFinished { get; init; }
		public required FlatStage[] Stages { get; init; }
		public required FlatRound[] Rounds { get; init; }
		public Dictionary<string, string[]>? Groups { get; init; }
		public required FlatGame[] Games { get; init; }
	}
}
