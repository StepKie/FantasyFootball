# Bulk Simulation & Data Model Redesign (#37)

A ground-up redesign of the persistence/data model in service of bulk
simulation. Bulk-sim revealed that the current `Competition` graph isn't
just *suboptimal* for the new feature — it's also been causing pain in
adjacent areas (#42 qualifier resolution caching, #43/#44 SQLite cost
and retirement, the F5 freeze on `/competitions`). Rather than bolting
bulk-sim onto the old shape, we're refactoring the underlying model and
storage so several open issues dissolve in the process.

## Goals

- **Bulk simulation** (#37): user clicks "Sim N", N tournaments run, each
  is fully persisted and individually browsable. Modes:
  - **Historical** — fixed actual draw, N runs vary in per-game RNG.
  - **Fixed Random** — random draw done once, N runs share the draw.
  - **Always Random** — random draw redone every iteration.
- **No information loss**: each of the N runs is a fully browsable
  competition (same UI as a single-sim today).
- **Efficient at scale**: sim hot path is O(games), not O(games × bracket
  depth). Serialization produces small payloads. Per-run persistence
  cost doesn't blow up linearly with previously-stored count.
- **Easy to add new competition definitions** without recompiling. WC
  2018, EM 2020, future formats — drop in a JSON file.

## Non-goals (this PR)

- IndexedDB swap. Stays on `LocalStorageRepository` for this PR; the
  flat model alone makes payloads 5-10× smaller, which fixes the F5
  freeze at current data sizes. IndexedDB lands as a separate PR when
  bulk-sim at N≥50 becomes the actual usage pattern.
- Live UI polish. The current `CompetitionSetup.razor` MVP is throw-away;
  it gets rewired in a final step once the backend is verified.
- Migrating users' existing LocalStorage data. We'll detect old-format
  blobs on load, log a warning, and treat them as empty. Users
  re-create competitions in the new model. (Acceptable because the
  deployed app is at 0.3.1, this is still early product.)

---

## Storage decision: LocalStorage now, IndexedDB later

| Browser-side option | Verdict | Why |
|---|---|---|
| **LocalStorage + flat model** | **This PR** | Flat model shrinks payloads 5-10×; current scale (<50 comps) fits comfortably; F5 freeze dissolves without a storage swap. |
| **IndexedDB** | Next PR | Per-row writes; no whole-bucket cost; future-proofs bulk-sim at N≥50. Mechanical swap once interface is solid. |
| **SQLite-WASM (raw, not the sqlite-net-pcl ORM)** | Not pursued | No JOIN queries planned; document-store fits our access pattern; ~1 MB WASM blob isn't worth the capability we don't use. |

**The `IRepository` interface stays stable across this swap.** Flat
records serialize via `System.Text.Json`; both `LocalStorageRepository`
(this PR) and a future `IndexedDbRepository` consume the same JSON. No
data-model change is needed for the storage swap later.

---

## Data model: flat-ish, ID-keyed, index-friendly

### Why the current graph hurts

- `Competition → Stages → Rounds → Games` + back-references on every
  level. With `ReferenceHandler.Preserve` each nested instance gets a
  JSON `$id`/`$ref`. A WC48 graph is ~500 KB serialized.
- `KoGame.HomeTeam` walks `HomeQualifier.Get() → Group.GetStandings()`
  on every access — O(M) per call per game (issue #42).
- `KoGame` having multiple `[OneToOne]` to the same child type triggered
  the SQLite cascade stomp (PR #41).
- The deep graph forces every consumer to know the hierarchy:
  `@foreach (stage in comp.Stages) @foreach (round in stage.Rounds) …`.

### Proposed shape

A `Competition` becomes a flat record with arrays + ID references. No
parent-child nav properties. Lookups are O(1) via indexed arrays.

```csharp
public sealed class Competition
{
    public int Id { get; init; }
    public string Title { get; init; } = "";                 // "WC 2026"
    public CompetitionType Type { get; init; }
    public int Year { get; init; }
    public string FormatId { get; init; } = "";              // "world-cup-48"
    public DateTime SimulationStart { get; set; }
    public DateTime? SimulationFinished { get; set; }

    /// Participants. Order is meaningful — TeamIds[i] is referenced as
    /// "team index i" elsewhere. Each ID is a key into the global Team
    /// registry (teams.json).
    public string[] TeamIds { get; init; } = [];

    /// Group assignments. GroupAssignments[i] holds the team IDs in
    /// group letter (char)('A' + i). For knockout-only formats this
    /// stays empty.
    public string[][] GroupAssignments { get; init; } = [];

    /// Stage metadata, declared up front by the competition definition.
    /// Order field controls display ordering; Games reference these by Id.
    public Stage[] Stages { get; init; } = [];

    /// Round metadata, declared up front by the competition definition.
    /// Round.StageId is a FK into Stages. Games reference these by Id.
    public Round[] Rounds { get; init; } = [];

    /// All games, in chronological order.
    public Game[] Games { get; init; } = [];
}

/// Tiny metadata record — single source of truth for stage display name
/// and ordering. No state, no contained games (those live in Competition.Games
/// with Game.StageId as the FK).
public sealed record Stage(string Id, string Name, int Order);

/// Tiny metadata record — single source of truth for round display name,
/// ordering, and stage assignment.
public sealed record Round(string Id, string Name, string StageId, int Order);

/// Sealed hierarchy on game KIND (Group vs KO) — drives polymorphic JSON
/// deserialization via System.Text.Json [JsonPolymorphic] / [JsonDerivedType].
/// State (scheduled vs played) is orthogonal: a nullable Result on the base.
public abstract class Game
{
    public int Id { get; init; }                             // explicit, set in the definition JSON; referenced by qualifiers (W{Id}, L{Id})
    public DateTime PlayedOn { get; init; }
    public string RoundId { get; init; } = "";               // FK into Competition.Rounds (StageId is implied via Round.StageId)
    public string? VenueId { get; init; }                    // key into venues.json
    public Result? Result { get; set; }                      // null = scheduled, non-null = played
}

public sealed class GroupGame : Game
{
    public string GroupLetter { get; init; } = "";
    public string HomeTeamId { get; init; } = "";            // known at competition start
    public string AwayTeamId { get; init; } = "";
}

public sealed class KoGame : Game
{
    public string HomeQual { get; init; } = "";              // qualifier intent ("A1", "W49") — immutable, set at competition start
    public string AwayQual { get; init; } = "";
    public string? HomeTeamId { get; set; }                  // qualifier resolution — null until upstream resolves, then cached
    public string? AwayTeamId { get; set; }
}

public sealed class Result
{
    public int HomeScore { get; init; }
    public int AwayScore { get; init; }
    public GameEnd Ending { get; init; }
    // Future expansion: int? Attendance, Goal[] Goals, Card[] Cards, …
}
```

### Mapping of game state to fields

| State | Type | `Result` | Team fields |
|---|---|---|---|
| Played | either | non-null | populated (resolved-and-cached for KO) |
| Scheduled — group | `GroupGame` | null | `HomeTeamId` / `AwayTeamId` set up-front |
| Scheduled — KO, upstream incomplete | `KoGame` | null | `HomeQual` / `AwayQual` set; `HomeTeamId` / `AwayTeamId` null |
| Scheduled — KO, upstream resolved | `KoGame` | null | qualifiers + cached `HomeTeamId` / `AwayTeamId` |

**Why both `HomeQual` and `HomeTeamId` on `KoGame`:** `HomeQual` is the
schedule-time *intent* (immutable, e.g. `"A1"` = group A's winner);
`HomeTeamId` is the *cached resolution* once the upstream qualifier
resolves. Storing both avoids the O(M) qualifier walk on every
`HomeTeam` access (which was the bug in #42). The cache is invalidated
on undo of any upstream game.

### Storage shape — denormalized

Each Competition stores its full schedule + games (~33 KB JSON for WC48).
At bulk-sim scale this duplicates the schedule across N runs. Estimated
storage at typical bulk-sim N=20: ~660 KB; at N=100: ~3.3 MB. Both
comfortably fit in LocalStorage's 5-10 MB quota.

**A normalized template-and-results split would save ~75% storage at
scale** (one template + N small result-only records). Not pursued now
because:
- The current scale doesn't need it; the cost is bounded.
- It complicates load (2-step lookup: Competition → Template → merge).
- Self-contained Competition records are simpler to reason about.
- Normalization is a clean retrofit when IndexedDB lands (the runtime
  API doesn't change, only the on-disk layout).

Tracked as a known future optimization; revisit alongside IndexedDB.

**Qualifier strings** are decoded by a small parser. The dash in
`W-n`/`L-n` disambiguates from group L placements (12-group formats
have a group L, so `L1` must mean "group L 1st place"; loser-of-game
uses the dashed form).

| Pattern | Meaning |
|---|---|
| `A1` / `L1` | Group A / L's 1st-place finisher (no dash) |
| `A2` / `B3` | Group A 2nd / B 3rd-place finisher |
| `A/B/F/G/I3` | Best 3rd-place finisher among groups A, B, F, G, I (expanded WC) |
| `W-49` | Winner of game with ID 49 (dash disambiguates from group placement) |
| `L-61` | Loser of game 61 (3rd-place match) |

Game IDs are explicit (set in the JSON definition), not array-position derived.

KO games' `HomeTeamId`/`AwayTeamId` are null when the qualifier hasn't
resolved yet. The resolver writes them in once standings/winners are
known. Persisted: yes — once resolved, it stays resolved.

### Computed views

These aren't stored — they're projections, defined as extension methods
on `Competition` for readability:

```csharp
public static class CompetitionExtensions
{
    public static IEnumerable<Game> GroupGames(this Competition c, string letter)
        => c.Games.Where(g => g.GroupLetter == letter);

    public static IEnumerable<Game> RoundGames(this Competition c, string roundId)
        => c.Games.Where(g => g.RoundId == roundId);

    public static Game? LastGame(this Competition c)
        => c.Games.LastOrDefault(g => g.State == GameState.FINISHED);

    public static Game? CurrentGame(this Competition c)
        => c.Games.FirstOrDefault(g => g.State != GameState.FINISHED);

    public static bool IsFinished(this Competition c)
        => c.Games.All(g => g.State == GameState.FINISHED);

    public static string? WinnerTeamId(this Competition c)
        => c.LastGame() is { } g
            ? (g.HomeScore > g.AwayScore ? g.HomeTeamId : g.AwayTeamId)
            : null;
}
```

Standings calculation: a pure function over `Game[]` filtered by group
letter. No mutable state, no caching needed for correctness (caching
becomes a perf concern, addressed in tests below).

### Global registries

Loaded once on app start, treated as immutable:

```jsonc
// teams.json
[
  { "id": "MEX", "name": "Mexico", "shortName": "MEX", "type": "national-men", "flag": "MX" },
  { "id": "BRA", "name": "Brazil", "shortName": "BRA", "type": "national-men", "flag": "BR" },
  …
]

// venues.json
[
  { "id": "azteca", "name": "Estadio Azteca", "city": "Mexico City", "country": "MX", "capacity": 87523 },
  { "id": "la-stadium", "name": "SoFi Stadium", "city": "Inglewood", "country": "US", "capacity": 70240 },
  …
]
```

**Elo** stays where it lives today — separate FIFA CSV, loaded per-year.
Not part of the Team registry (Elo changes over time; team identity
doesn't).

### Competition definition format

One JSON file per (Type, Year). Reused by `HistoricalSpec`.

```jsonc
// competitions/wm-2026.json
{
  "id": "wm-2026",
  "title": "World Cup 2026",
  "type": "WM",
  "year": 2026,
  "formatId": "world-cup-48",

  "groups": {
    "A": ["MEX", "RSA", "POR", "USA"],
    "B": ["CAN", "BEL", "ESP", "ARG"],
    …
  },

  "games": [
    {
      "playedOn": "2026-06-11T20:00:00",
      "stageId": "group",
      "roundId": "group-r1",
      "groupLetter": "A",
      "venueId": "azteca",
      "home": "MEX",
      "away": "RSA"
    },
    …
    {
      "playedOn": "2026-06-28T20:00:00",
      "stageId": "ko",
      "roundId": "r32",
      "venueId": "la-stadium",
      "homeQual": "A1",
      "awayQual": "B2"
    }
  ]
}
```

For "Fixed Random" and "Always Random" modes, the same file is used as
the **schedule template**; the `groups` field is **regenerated by the
draw algorithm** and substituted before the spec materializes into a
Competition.

---

## `CompetitionSpec` hierarchy

All three specs share **the same schedule template** — `definitionId`
points to the canonical competition JSON file (e.g. `wm-2026.json`),
which defines stages, rounds, dates, venues, and qualifier wiring. The
specs differ only in **how teams are populated into that schedule**.

```csharp
public abstract class CompetitionSpec
{
    /// Reference to the schedule template (e.g. "wm-2026", "wm-1998").
    /// All Build() implementations load this JSON and use its schedule;
    /// the variance is in team assignment.
    public string DefinitionId { get; }

    /// Build a fresh Competition graph ready to be simulated.
    public abstract Competition Build();
}

public sealed class HistoricalSpec(string definitionId) : CompetitionSpec
{
    // Build() uses the historical groups from the definition file verbatim.
}

public sealed class CustomLineupSpec(
    string definitionId,
    IReadOnlyDictionary<string, string[]> groups) : CompetitionSpec
{
    // Build() loads the schedule and substitutes the user-supplied groups.
}

public sealed class RandomLineupSpec(
    string definitionId,
    IDrawAlgorithm draw) : CompetitionSpec
{
    // Build() loads the schedule, calls draw.Draw(...) for a fresh team
    // assignment, and substitutes those groups. Each call redraws.
}
```

Examples — note all three reference the same `wm-1998` schedule:

```csharp
new HistoricalSpec("wm-1998")                  // Brazil, France, … as they actually were
new CustomLineupSpec("wm-1998", myGroups)      // same dates + venues + qualifier wiring; my chosen teams
new RandomLineupSpec("wm-1998", drawAlgo)      // same dates + …; fresh random draw on each Build()
```

`CompetitionSpec` is named differently from `CompetitionSetup.razor`
(the page) to avoid collision. The page still exists; it's the UI for
*choosing and configuring* a Spec.

### `IDrawAlgorithm`

```csharp
public interface IDrawAlgorithm
{
    /// Returns a fresh group assignment for the given competition.
    /// Implementation decides: team pool, drawing strategy, how teams
    /// map to the format's group structure.
    IReadOnlyDictionary<string, string[]> Draw(string definitionId);
}
```

The current implementation, `GlobalRegistryUniformDraw`, mirrors today's
`GroupFactory.DrawRandom()` behavior — pulls from the global team
registry filtered by year/type, uniformly random across group slots.

**Designed for future variance** (called out so we don't paint ourselves
into a corner): the interface returns groups but doesn't expose the
pool or strategy choice — each implementation owns both decisions.
Future implementations like `FifaPotBasedDraw` (seeding by confederation
+ pots) or `RestrictedPoolDraw` (user-defined eligible-teams subset)
slot in as alternative `IDrawAlgorithm`s without touching the spec
hierarchy. If pool/strategy variance grows complex enough to warrant a
split (`ITeamPool` + `IDrawStrategy`), that's a clean retrofit later;
the spec layer doesn't change.

---

## `BulkSimRunner`

```csharp
public sealed class BulkSimRunner
{
    public async Task<IReadOnlyList<Competition>> RunAsync(
        CompetitionSpec spec,
        int count,
        IRepository repo,
        IProgress<int>? progress = null,
        CancellationToken cancel = default)
    {
        var results = new List<Competition>(count);
        for (int i = 0; i < count; i++)
        {
            cancel.ThrowIfCancellationRequested();
            var c = spec.Build();
            var sim = new CompetitionSimulator(c, repo) { Quiet = true };
            await sim.Simulate();
            await repo.SaveAsync(c);
            results.Add(c);
            progress?.Report(i + 1);
            await Task.Yield();
        }
        return results;
    }
}
```

For `HistoricalSpec` / `CustomLineupSpec`, every iteration's `Build()`
returns an equivalent fresh graph (same groups, same schedule). For
`RandomLineupSpec`, each iteration redraws.

Aggregates over the N results are extension methods:

```csharp
public static class BulkSimAggregations
{
    public static IReadOnlyDictionary<string, int> WinnerFrequencies(
        this IReadOnlyList<Competition> runs)
        => runs.GroupBy(c => c.WinnerTeamId()!)
               .Where(g => g.Key is not null)
               .ToDictionary(g => g.Key, g => g.Count());

    public static IReadOnlyDictionary<string, int> AppearancesInRound(
        this IReadOnlyList<Competition> runs, string roundId)
        => runs.SelectMany(c => c.RoundGames(roundId))
               .SelectMany(g => new[] { g.HomeTeamId, g.AwayTeamId })
               .Where(t => t is not null)
               .GroupBy(t => t!)
               .ToDictionary(g => g.Key!, g => g.Count());
    …
}
```

---

## Testing strategy

Mandatory for this work: every new path is covered by tests. **Two
classes of test go beyond normal unit/integration: performance assertions
and head-to-head against the old code path while it still exists.**

### Unit tests

- `QualifierParserTests` — `A1`, `A2`, `W49`, `L61`, `A/B/F3` all parse
  to the expected resolution intent.
- `CompetitionExtensionsTests` — `GroupGames`, `LastGame`, `WinnerTeamId`,
  `IsFinished`, standings calc all return expected values for hand-built
  Competition records.
- `CompetitionDefinitionLoaderTests` — JSON → `Competition` (template
  shape, before sim).
- `HistoricalSpec` / `CustomLineupSpec` / `RandomLineupSpec` build
  behavior.
- `BulkSimAggregations` — winner frequencies, round appearances etc.
  against hand-built result lists.

### Integration tests

- `BulkSimRunner_HistoricalMode_PersistsN` — N=5 WC2026, every persisted
  comp has a winner, count in repo is N + prior.
- `BulkSimRunner_FixedRandomMode_AllRunsShareLineup` — N=5, all 5 have
  identical participants.
- `BulkSimRunner_AlwaysRandomMode_LineupsVary` — N=5, ≥ 2 have different
  participants.
- `BulkSimRunner_Persisted_RoundtripsCleanly` — save, reload,
  reload-matches-original.

### Performance tests (assertion-gated, opt-in)

These fail the build if numbers regress. Tagged
`[Trait("Category", "Performance")]` so they run on explicit
opt-in (`dotnet test --filter "Category=Performance"`) — keeps the
default `dotnet test` fast for normal dev iteration but still gates
the architectural promise in dedicated CI runs.

If a perf test bites during iteration (excessive duration before the
backend's stable enough to actually hit the targets), temporarily
`[Skip]` it with a reason or move it to a slower category — never
loosen the threshold to make it pass.

- `FlatModel_WC48_SimulationCompletesUnder150ms` — full WC48 sim on
  the new flat model takes < 150 ms wall-clock (today's number is
  100 ms; 50 % headroom for orchestration overhead).
- `FlatModel_WC48_SerializedJsonUnder100KB` — finished WC48 Competition
  serializes to < 100 KB JSON (today's graph is ~500 KB).
- `BulkSimRunner_N20_WC48_CompletesUnder8s` — N=20 WC48 runs
  (sim + persist) finish in < 8 s. Realistic expectation: sim ≈ 2 s,
  cumulative LocalStorage persistence ≈ 1 s, with ~5 s of headroom
  for GC / browser variance. If we blow this, persistence layer is the
  culprit (quadratic cumulative bytes from whole-bucket rewrites).
- `BulkSimRunner_N100_WC48_CompletesUnder120s` — N=100 explicitly
  exercises the LocalStorage quadratic-write-cost regime. Total
  cumulative bytes written ≈ 165 MB (sum of bucket sizes from 1 to 100
  × 33 KB). Expected wall-clock 60-120 s. **If this fails or runs
  excessively long, it's the signal to land IndexedDB.** Threshold
  intentionally loose pending real numbers; tighten after first run.

### Head-to-head tests (parity + superiority vs old model)

While both old and new code paths coexist, prove the new path is *at
least equivalent* on outcomes and *strictly better* on performance.
These tests disappear when the old path is deleted; the design doc
calls out that explicitly.

- `OldVsNew_StandingsForIdenticalGames_AreEquivalent` — feed the same
  set of game results into both an old `Standings.CreateFrom(games)`
  call and a new `competition.GroupStandings("A")` extension. Resulting
  TeamRecord lists must match by team identity + Points / GD / GF.
- `OldVsNew_FullCompetitionSimulation_ProducesValidR16` — sim a WC32 on
  both paths; assert each produces a valid R16 (16 distinct teams,
  group winners in top half / runners-up in bottom half). Doesn't
  require *identical* outcomes (RNG isn't seeded), but invariants must
  hold on both.
- `OldVsNew_SimulationSpeed_NewIsAtLeast2xFaster` — measure the
  per-comp sim cost in the same test method; assert
  `newElapsed < oldElapsed / 2`. If the new model isn't faster, the
  refactor isn't earning its keep.
- `OldVsNew_SerializedSize_NewIsAtLeast5xSmaller` — same setup, both
  paths persisted, compare JSON byte length. New must be ≥ 5× smaller.
- `OldVsNew_QualifierResolution_NewIsAtLeast10xFaster` — measure
  KoGame.HomeTeam access cost in tight loop on both. New (`Game.HomeTeamId`)
  is a field read; old goes through `Get() → GetStandings()`. Expected
  ratio ≥ 10×.

These tests are intentionally **strict**: better to fail loudly during
development than to land a refactor that didn't actually improve what
it set out to improve. If a number doesn't pan out, we know to revisit
the design before deleting the old path.

---

## Implementation order

1. **Domain model + parser + definition loader.** Pure C#, no UI, no
   storage swap. Land with unit tests.
2. **Persistence path for the new model.** New `Competition` shape stored
   via existing `IRepository` (LocalStorage). Co-exists with old; new
   model has a distinct bucket key so old data is undisturbed.
3. **`CompetitionSpec` hierarchy.** Wired to the definition loader.
4. **`BulkSimRunner` + aggregator.** Pure backend.
5. **Integration tests + perf tests + head-to-head tests.** Every
   acceptance criterion above.
6. **UI cutover.** `CompetitionSetup.razor` rewires to spec-based flow;
   bulk-sim controls (count + mode picker) land on the same page;
   competition list / detail pages rebind to the new shape. Single-sim
   flow uses `HistoricalSpec.Build()` or `CustomLineupSpec.Build()`
   instead of factories.
7. **Delete old paths.** Old factories, old `Competition` graph fields,
   old `Standings.CreateFrom`, `TeamRecord` (or repurposed) — gone.
   Head-to-head tests deleted in the same commit. Repo bucket cleanup
   for old format.

Each step is its own commit (or small commit group) on the same branch.
PR opens once step 5 is green; UI work lands on top.

---

## Acceptance checklist

Backend:

- [ ] All unit tests green.
- [ ] All integration tests green.
- [ ] All performance tests green and stay green.
- [ ] All head-to-head tests prove parity + superiority.
- [ ] WC2026 / WC2022 / EM2024 historical definitions in JSON files,
  matching the structure produced by the old C# factories.
- [ ] BulkSimRunner with `IProgress<int>` integration.

UI:

- [ ] Single-sim flow unchanged behaviorally (regression test).
- [ ] Bulk-sim controls (count + mode picker) on Setup page.
- [ ] Progress visible during bulk runs.
- [ ] All N runs visible in /competitions Finished tab.
- [ ] Each run individually browsable, same UI as a single sim today.

Cleanup:

- [ ] Old factories deleted.
- [ ] Old `Competition.Stages` / nested model deleted.
- [ ] `Repository.cs` workaround in `Core` deleted (PR #41's
  PreInsert/Restore code goes — without nested `[OneToOne]` cascades,
  none of that is needed).
- [ ] `KoGame.cs` 4-FK collapse comment goes — no longer needed.
- [ ] Head-to-head tests deleted.

## Known follow-ups (out of scope of this work)

- **IndexedDB storage** (next PR after this one lands). Mechanical
  swap of `LocalStorageRepository` → `IndexedDbRepository`. Resolves
  scaling concerns at N≥50 and any remaining list-page load latency.
- **Aggregate UI polish**. The current MVP results table is functional;
  later passes can add distributions, finalist appearances, group exit
  tables, charts.
- **Batch grouping** in /competitions list. Once 100-sim batches are
  routine, optionally add a `BatchId` tag + collapsible group rows.
- **Premier League / domestic-league setups**. The same JSON definition
  format handles them; just need a different `formatId`
  (`round-robin-2-leg`) and the right group/round structure. No code
  changes once formats are flexible.
