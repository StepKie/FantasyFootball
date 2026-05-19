# UI polish PR 1 — failure log

Session handoff doc. Read this first if you're picking up the polish work
cold. Companion to `docs/ui-polish-plan.md` (the strategic plan) — this
file is the tactical "what bugs we hit, what's fixed, what's still
lurking" log.

**Branch**: `feature/ui-polish-1-ia-speed` (pushed as PR #25 → `develop`).
**State at last update (2026-05-19)**: 13 commits ahead of `develop`.
Build is clean (0 errors). Tests: 34 passing / 6 failing — the 6 are
the pre-existing greedy 3rd-place tournament-sim tests (Outstanding A,
issue #12), unchanged by any work in this PR.

## What PR 1 was supposed to do

Information-architecture cleanup + a per-session speed control on the
competition detail page. Items and their status are checkboxed in
`ui-polish-plan.md § PR 1`. **Only one item still pending**: merge the
`/competitions` and `/statistics` pages into a single page with
Active / Finished tabs.

Everything else in the original PR 1 scope shipped, plus a long list
of bug fixes that surfaced while we tested the IA pass against a real
2026 WC simulation. The bugs are this doc's main subject.

## Branch commit log

```
9b870fc [Ignore] Qualifier.QualifiedTeam + swallow 3rd-place greedy failures
9e2c90a Disable speed toggle while sim is in flight
0907827 Add factory-output and post-deserialize round-trip tests
3793d59 Add LocalStorage round-trip tests; in-process repro of Web bug not reproducing (yet)
c118d35 Yield in Instant sim mode so the UI stays responsive
737df22 Fix premature 3rd-place resolve + tests for both qualifier bugs
777d821 Fix empty-after-create + quarantine corrupt LocalStorage on F5 crash
7268c4e Drop invalid Dense attribute from MudToggleGroup (MUD0002 warning)
a579208 UI polish PR 1 (2/N): delete-refresh, About page, picker widths, speed control
48fad3a UI polish PR 1 (1/N): drop Home page, drop huge H1s, add plan doc
187ea7c Fix third-place match placeholder showing 'Winner' instead of 'Loser'
```

## Bugs encountered, in order

### 1. Third-place match shows "Winner Semifinal 1/2" instead of losers — **fixed (187ea7c)**

`GameQualifier.GetPlaceholder()` hardcoded `Res.Winner` even when
`LoserQualifies` was true. `Get()` already respected the flag; the
placeholder branch didn't. Test pinned in `QualifierTests`.

### 2. Competitions list empty after creating a competition — **fixed (777d821)**

`CompetitionsViewModel` listened for `CompetitionFinishedMessage`,
`CompetitionDeletedMessage`, and `DataResetMessage`, but nobody fired a
"created" message. Added `CompetitionCreatedMessage`, broadcast from
`CompetitionSetupViewModel.Create()`.

### 3. `MetadataReferenceNotFound` JSON corruption — **FIXED (real root cause found)**

The symptom: F5 after creating a competition and sim'ing a couple
games → `JsonException: MetadataReferenceNotFound, NNN, Path: $.$values[0].Stages.$values[0].Groups.$values[1]`,
blank page.

**Real root cause (confirmed 2026-05-19):** `CompetitionSetupViewModel.Groups`
is a single `List<Group>` instance that only refreshes on year change
(via `ResetToHistoricTeams`). Creating two competitions of the same
type+year back-to-back passes the same `List<Group>` (and the same
Group instances) to two separate `CompetitionFactory.Create()` calls.
Both Competitions end up with `Stage.Groups` pointing at the same
Group instances. `WireBackReferences` for Comp #2 overwrites every
`group.Stage` back-pointer (set moments earlier by Comp #1's pass)
to point at Comp #2's Stage.

Comp #1's serialize then walks `Stages[0].Groups[0].Stage` → finds
Comp #2's Stage (a different instance from the parent Stage it just
$id'd), writes the full Stage body inline, which recursively walks
`Stage.Competition` → Comp #2 inline, `Comp #2.Stages` → both stages
inline including the K.O. Phase with all its KoGames and qualifiers.
Group $ids end up assigned deep inside this nested expansion. The
outer `Stages[0].Groups[1..N]` then emits `$ref:<deep-id>` to those
in-nested Groups — but on deserialize, the deep-id $ids haven't been
encountered by the parser at the position where the $ref lands.
**Result:** valid-looking JSON that's actually a forward reference in
Preserve mode, blowing up on the next load.

**Fix:** defensive clone in `CompetitionFactory.Create()`:
```csharp
Groups = Groups.Select(g => new Group { Name = g.Name, Teams = [.. g.Teams] }).ToList();
```
Each `Create()` now owns its Group instances. Teams remain shared
(same `Team` references) — only the Group containers are fresh.

**Repro test:** `LocalStorageSerializationTests.TwoCompetitions_SharingFactoryGroups_RoundTrips`.
Fails on tip pre-fix, passes on tip post-fix.

**Earlier hypothesis was wrong.** Commits 777d821 + 9b870fc reduced
the surface (`[Ignore]` on `Qualifier.QualifiedTeam` stopped a different
greedy-3rd-place throw mid-serialize), but the underlying shared-Groups
issue was still there. The corruption recurred on the very next
two-competitions-same-type+year flow.

**Quarantine still in place.** `LocalStorageRepository.LoadBucket` still
moves any corrupt blob to a `fantasy-football:Competition:corrupt-{ts}`
key and logs to console, so future serialization bugs (if any) won't
brick the page.

### 4. Premature third-place qualifier resolution — **fixed (737df22)**

`GroupQualifier.Get()` gated the 3rd-place branch on per-group
`Group.IsFinished`. The moment any single group finished, it called
`ExpandedWorldCupFormat.ResolveThirdPlaceQualifier` — which compares
3rd-place finishers across **every** group and throws if any group
hasn't reported yet. Fixed by gating on
`stage.Groups.All(g => g.IsFinished)`.

**Subtle prerequisite**: `Group.IsFinished` is vacuously true for a
group with zero games. In the live app every group has scheduled games
at creation time, so this isn't reached. The test in
`QualifierTests.BuildQualifier` adds SCHEDULED games for "unfinished"
groups so the assertion is meaningful.

### 5. Instant-mode UI freeze — **fixed (c118d35)**

`Quiet` mode in `CompetitionSimulator.SimulateGame` skipped both the
`MessageBus.Send` per game **and** the `await Task.Delay`. The Delay
was the only yield point, so a 100-game tournament ran as one
synchronous chunk on the WASM main thread — `IsBusy=true` never
rendered because Blazor never got a chance to commit it. Replaced
the missing delay with `await Task.Yield()`. ~zero overhead per game,
~100 ms over a 48-team WC tournament — vs the multi-second freeze.

### 6. `Qualifier.QualifiedTeam` throwing during JSON serialize — **fixed (9b870fc)**

The real prize. Stack trace:

```
at Qualifier.get_QualifiedTeam
at System.Text.Json.Serialization.Metadata.JsonPropertyInfo`1.GetMemberAndWriteJson
at ObjectDefaultConverter`1<GroupQualifier>.OnTryWrite
```

`Qualifier.QualifiedTeam` is a UI-convenience getter
(`Get() ?? GetPlaceholder()`). It was **not** marked `[Ignore]`, so STJ
called it during every serialization of a Qualifier. For
`GroupQualifier` with `FinalPlacement=3` and a fully-finished group
stage, `Get()` reaches `ExpandedWorldCupFormat.ResolveThirdPlaceQualifier`
which can throw an `InvalidOperationException` on greedy-allocation
failure (issue #12 — known shortcoming, real fix is FIFA's 495-scenario
lookup table).

Two fixes layered:
- `[Ignore]` on `Qualifier.QualifiedTeam`. STJ no longer calls it.
- `GroupQualifier.Get()` wraps the third-place resolve in
  `try { … } catch (InvalidOperationException) { return null; }`. The
  KO-bracket render that also calls `Get()` via `KoGame.HomeTeam` now
  falls back to a placeholder instead of crashing the renderer.

### 7. Speed-toggle change mid-sim — **guarded (9e2c90a), but it wasn't a race**

I initially called this a race in the commit message. **It wasn't.**
The reported "click Instant during a Normal sim → crash" was the same
`Qualifier.QualifiedTeam` exception above — it just happened to fire
at the moment a sim batch ended, which coincided with the user
clicking Speed.

The defensive `Disabled="ViewModel.IsBusy"` on the speed toggle is
still a reasonable UX guard (don't change `GameDelay` / `Quiet`
mid-batch), but it doesn't fix a bug. **Leave the guard; the commit
message overclaimed.**

## Outstanding (known but not fixed in this PR)

### A. Issue #12 — greedy 3rd-place allocation can fail

`ExpandedWorldCupFormat.ResolveThirdPlaceQualifier` uses a
most-constrained-first greedy algorithm that can fail to fit every
slot. Hidden by the placeholder fallback in #6 — the user sees a
"TBD" badge where a real team should appear. **Real fix**: FIFA's
official 495-scenario lookup table per the TODO at the top of
`ExpandedWorldCupFormat.cs`. Out of scope for the polish PR.

### B. JSON bloat from un-`[Ignore]`'d computed getters — **investigated, NOT fixed (revert in working memory)**

Audit during this session identified four computed getters on
persisted types that STJ serializes (unnecessarily):
- `Confederation.Logo`
- `Team.Logo`
- `Country.NationalTeam` (returns a NEW Team each call)
- `KoGame.HomeTeam` / `KoGame.AwayTeam` (overrides without `[Ignore]`)

Attempted to add `[Ignore]` to all four — broke 6 full-tournament
simulation tests because **the same SQLite `[Ignore]` attribute also
removes columns from the SQLite schema**. The audit changes were
reverted (uncommitted). The fix is structural: separate JSON's
`[JsonIgnore]` from SQLite's `[Ignore]`. **Not a crash risk** —
just JSON bloat. Belongs in a follow-up cleanup PR, not this polish
work.

### C. (done) Merge Competitions + Statistics — PR 1 is now complete

Landed: PR #25 on `feature/ui-polish-1-ia-speed`. Single `/competitions`
page with Active / Finished tabs; all-time TeamRecord aggregate below
the Finished list. `/statistics` route + NavMenu link removed.

## Diagnostic affordances we added

- **`LocalStorageRepository.LoadBucket` quarantine** (777d821) — on
  any `JsonException`, the corrupt blob is moved to a
  `fantasy-football:{Type}:corrupt-{yyyyMMddHHmmss}` key, the bucket
  starts empty, and the browser console logs blob length + error
  message. Reset Database (Settings) clears both. This caught the
  shared-Groups corruption (#3) on 2026-05-19 and preserved the
  diagnostic blob that pinned the root cause.
- **`LocalStorageSerializationTests`** (3793d59, 0907827, + new
  `TwoCompetitions_SharingFactoryGroups_RoundTrips`) — repro scaffold
  using the actual factory output (not SQLite-rehydrated). The new
  test fails on tip pre-fix, passes post-fix; pins the
  shared-Groups regression.

## What I'm NOT confident about

- The `KoGame.HomeTeam` / `AwayTeam` overrides without `[Ignore]`
  still serialize placeholder Team objects into the JSON. Round-trip
  works (the override is read-only, deserialize discards, getter
  recomputes), but it's noise. Outstanding (B).

## Quick start (for tomorrow / next session)

```bash
git checkout feature/ui-polish-1-ia-speed
git pull
dotnet test src/FantasyFootball.Tests/FantasyFootball.Tests.csproj
dotnet run --project src/FantasyFootball.Web --launch-profile https
# → https://localhost:7140
```

Next coding task: once PR #25 merges, start PR 2 (Identity) on a
fresh branch off `develop`.
