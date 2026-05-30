using FantasyFootball.Data;

namespace FantasyFootball.Models;

/// <summary>
/// Competition aggregate. Stages, Rounds, GroupAssignments,
/// and Games live as sibling arrays — no parent-child navigation chain.
/// Lookups across them go through Ids (Game.RoundId → Round,
/// Round.StageId → Stage, etc.) or extension methods on
/// Competition.
///
/// Self-contained: a Competition can be serialized in isolation.
/// Bulk simulation produces N independent Competitions, each fully
/// browsable (no shared template reference). At scale (~N≥100) this
/// duplicates the schedule across runs — a known optimization
/// (normalized template + results-only-per-run) deferred until IndexedDB
/// lands.
///
/// </summary>
public sealed class Competition
{
	/// <summary>
	/// Repository-assigned identifier. <c>0</c> for an unsaved competition;
	/// the repo flips this to a positive integer on first save. Not part
	/// of the on-disk JSON shape — repository keys it externally.
	/// </summary>
	public int Id { get; set; }
	public required string Title { get; init; }
	public required CompetitionType Type { get; init; }
	public required int Year { get; init; }

	/// <summary>
	/// Identifier of the schedule template this competition was built
	/// from (e.g. "wm-2026"). Stable across all three CompetitionSpec
	/// modes (Historical / CustomLineup / RandomLineup) — same template,
	/// different team-assignment strategies.
	/// </summary>
	public required string DefinitionId { get; init; }

	/// <summary>
	/// Format identifier (e.g. "world-cup-48"). Resolved at sim time
	/// against an in-code format registry that knows the bracket shape
	/// and third-place advancement rules.
	/// </summary>
	public required string FormatId { get; init; }

	/// <summary>
	/// Wall-clock timestamp when the simulator was started on this
	/// competition. Null for a freshly-loaded definition that hasn't
	/// been simulated yet; set by the sim entry point.
	/// </summary>
	public DateTime? SimulationStart { get; set; }

	/// <summary>
	/// Wall-clock timestamp when the simulator finished. Null while
	/// in progress (or never simulated).
	/// </summary>
	public DateTime? SimulationFinished { get; set; }

	/// <summary>
	/// Group assignments. GroupAssignments[i] holds the team IDs in group
	/// letter (char)('A' + i). Empty for knockout-only formats. The
	/// full participant list is the flat union — see
	/// <see cref="CompetitionExtensions.AllTeamIds"/>.
	/// </summary>
	public required string[][] GroupAssignments { get; init; }

	/// <summary>Stage metadata, declared up-front by the competition definition.</summary>
	public required Stage[] Stages { get; init; }

	/// <summary>Round metadata, declared up-front by the competition definition.</summary>
	public required Round[] Rounds { get; init; }

	/// <summary>All games, in chronological order.</summary>
	public required Game[] Games { get; init; }

	/// <summary>
	/// Name of the <see cref="EloSet"/> the simulator should use for this
	/// competition (e.g. <c>"2018"</c>, <c>"Current"</c>, <c>"Germany-OP"</c>).
	/// Null = use the legacy fallback chain (year-matched if it exists,
	/// otherwise the UI's active EloSet). Set by the setup picker; persists
	/// across saves so the sim path is deterministic per competition.
	/// </summary>
	public string? EloSetName { get; set; }
}
