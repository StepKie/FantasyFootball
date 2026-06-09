using System.Text.Json;
using FantasyFootball.Models;
using FantasyFootball.Repositories;
using FantasyFootball.Services;
using Microsoft.JSInterop;

namespace FantasyFootball.Web.Services;

/// <summary>
/// IndexedDB-backed flat-competition store. Replaces the LocalStorage store to
/// escape the ~5 MB Web Storage per-origin cap — IndexedDB draws from the
/// browser's disk-based quota, so bulk sims of thousands of competitions fit.
/// Marshals through the <c>window.ffIdb</c> JS helper.
///
/// Two object stores keyed by repo id: the full competition JSON and a tiny
/// <see cref="CompetitionSummary"/> projection, written together on every save.
/// The list reads only summaries (<see cref="GetAllSummariesAsync"/>) so it
/// never ships or parses every full season; the full competition is loaded only
/// when one is opened (<see cref="GetAsync"/>) or for the overall standings
/// (<see cref="GetAllAsync"/>). JSON shape and round-trip match
/// <see cref="InMemoryCompetitionRepository"/>.
/// </summary>
public sealed class IndexedDbCompetitionRepository : ICompetitionRepository
{
	readonly IJSRuntime _js;

	// Monotonic id counter; seeded once from existing keys, then bumped synchronously before each async write so a concurrent save (bulk sim + user click) can't collide on an id.
	int _nextId;

	// Cache of the bulk read (overall standings + cross-competition H2H); cleared on any write so the next read reflects it.
	IReadOnlyList<Competition>? _cachedAll;

	public IndexedDbCompetitionRepository(IJSRuntime js)
	{
		_js = js;
	}

	public async Task<int> SaveAsync(Competition competition)
	{
		if (_nextId == 0)
		{
			var keys = await _js.InvokeAsync<int[]>("ffIdb.keys");
			if (keys.Length > 0) { _nextId = keys.Max(); }
		}
		if (competition.Id == 0)
		{
			competition.Id = ++_nextId;
		}

		await _js.InvokeVoidAsync("ffIdb.save",
			competition.Id,
			FlatJson.Serialize(competition, compact: true),
			SerializeSummary(CompetitionSummary.Of(competition)));

		_cachedAll = null;
		return competition.Id;
	}

	public async Task<Competition?> GetAsync(int id)
	{
		var json = await _js.InvokeAsync<string?>("ffIdb.get", id);
		if (string.IsNullOrEmpty(json)) { return null; }
		var c = CompetitionDefinitionLoader.Load(json);
		c.Id = id;

		return c;
	}

	public async Task<IReadOnlyList<Competition>> GetAllAsync()
	{
		if (_cachedAll is not null) { return _cachedAll; }
		// One transaction so keys and values are a consistent snapshot — a delete mid-read can't misalign id[i] with payload[i].
		var entries = await _js.InvokeAsync<JsonElement>("ffIdb.entries");
		var ids = entries.GetProperty("keys").Deserialize<int[]>()!;
		var payloads = entries.GetProperty("values").Deserialize<string[]>()!;
		var result = new List<Competition>(ids.Length);
		for (int i = 0; i < ids.Length; i++)
		{
			if (string.IsNullOrEmpty(payloads[i])) { continue; }
			var c = CompetitionDefinitionLoader.Load(payloads[i]);
			c.Id = ids[i];
			result.Add(c);
		}

		_cachedAll = result;
		return result;
	}

	public async Task<IReadOnlyList<CompetitionSummary>> GetAllSummariesAsync()
	{
		var payloads = await _js.InvokeAsync<string[]>("ffIdb.summaries");
		var summaries = payloads.Select(DeserializeSummary).ToList();

		await BackfillMissingSummaries(summaries);

		return summaries;
	}

	public async Task DeleteAsync(int id)
	{
		await _js.InvokeVoidAsync("ffIdb.remove", id);
		_cachedAll = null;
	}

	public async Task<int> CountAsync() =>
		(await _js.InvokeAsync<int[]>("ffIdb.summaryKeys")).Length;

	public async Task ResetAsync()
	{
		await _js.InvokeVoidAsync("ffIdb.clear");
		_nextId = 0;
		_cachedAll = null;
	}

	// Competitions persisted before the summary store existed have a full payload but no summary. Derive the missing ones once from the full store; every later save keeps both in sync, so this no-ops thereafter.
	async Task BackfillMissingSummaries(List<CompetitionSummary> summaries)
	{
		var fullKeys = await _js.InvokeAsync<int[]>("ffIdb.keys");
		if (summaries.Count == fullKeys.Length) { return; }

		var known = summaries.Select(s => s.Id).ToHashSet();
		foreach (var id in fullKeys.Where(k => !known.Contains(k)))
		{
			var c = await GetAsync(id);
			if (c is null) { continue; }
			var summary = CompetitionSummary.Of(c);
			await _js.InvokeVoidAsync("ffIdb.setSummary", id, SerializeSummary(summary));
			summaries.Add(summary);
		}
	}

	static string SerializeSummary(CompetitionSummary s) => JsonSerializer.Serialize(s, FlatJson.CompactOptions);

	static CompetitionSummary DeserializeSummary(string json) => JsonSerializer.Deserialize<CompetitionSummary>(json, FlatJson.CompactOptions)!;
}
