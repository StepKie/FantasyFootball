namespace FantasyFootball.Models;

/// <summary>
/// Recipe for instantiating a fresh, unsimulated <see cref="Competition"/>.
/// The three subtypes correspond to the bulk-simulation modes:
///
/// <list type="table">
///   <listheader>
///     <term>Spec</term>
///     <description>Run interpretation when N&gt;1</description>
///   </listheader>
///   <item>
///     <term><see cref="HistoricalSpec"/></term>
///     <description>All N runs use the same historical lineup baked into
///       the definition file. Differences across runs come purely from
///       per-game RNG.</description>
///   </item>
///   <item>
///     <term><see cref="CustomLineupSpec"/></term>
///     <description>"Fixed Random" — the UI draws teams once up front
///       and reuses the same lineup for all N runs. Again, only the
///       per-game RNG differs.</description>
///   </item>
///   <item>
///     <term><see cref="RandomLineupSpec"/></term>
///     <description>"Always Random" — each of N runs gets a fresh draw.
///       Both lineup and per-game RNG vary across runs, so the
///       aggregator sees a much wider distribution of outcomes.</description>
///   </item>
/// </list>
///
/// Specs are short-lived: the user picks one, the runner materializes
/// it N times into Competitions, and the spec itself is discarded.
/// </summary>
public abstract record class CompetitionSpec
{
	public required string DefinitionId { get; init; }
}

/// <summary>
/// "Use the teams the definition file already specifies." For a
/// finished historical competition (e.g. WC2022), this is the real
/// 32-team field that actually played.
/// </summary>
public sealed record class HistoricalSpec : CompetitionSpec;

/// <summary>
/// "Use exactly these teams in these groups." Source is typically a
/// single random draw the UI did once up front (Fixed Random mode),
/// but can also be a hand-picked what-if lineup.
///
/// <para><c>Groups[i]</c> holds the team IDs in group letter
/// <c>(char)('A' + i)</c>, matching <see cref="Competition.GroupAssignments"/>.</para>
/// </summary>
public sealed record class CustomLineupSpec : CompetitionSpec
{
	public required string[][] Groups { get; init; }
}

/// <summary>
/// "Draw a fresh lineup per run." The runner calls
/// <see cref="DrawAlgorithm"/> once per simulated competition, so every
/// run starts from different teams.
/// </summary>
public sealed record class RandomLineupSpec : CompetitionSpec
{
	public required IDrawAlgorithm DrawAlgorithm { get; init; }
}
