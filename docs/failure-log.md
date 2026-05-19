# UI polish PR 1 — failure log

Session handoff doc. Read this first if you're picking up the polish work
cold. Companion to `docs/ui-polish-plan.md` (the strategic plan) — this
file is the tactical "what bugs we hit, what's fixed, what's still
lurking" log.

**Branch**: `feature/ui-polish-1-ia-speed` (pushed; target: `develop`).
**State at handoff**: 11 commits ahead of `develop`. Build is clean
(0 errors). Tests pass (10/10 in the qualifier + serialization suites;
the full suite has not been re-run since the audit revert — should be
~39/39 again).

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

### 3. `MetadataReferenceNotFound` JSON corruption — **almost certainly fixed, defensively contained (777d821 + 9b870fc)**

The symptom: F5 after creating a competition and sim'ing a couple
games → `JsonException: MetadataReferenceNotFound, NNN, Path: $.$values[0].Stages.$values[0].Groups.$values[1]`,
blank page.

Inspection of the corrupt JSON (`fantasy-football:Competition:corrupt-…`)
showed Group[0].Stage being serialized as a **brand-new Stage object**
(`$id:7`) rather than `$ref:"4"` to the parent Stage. This duplicated
the entire Stage subtree (Rounds, Games, …) and pushed the eventual
`$id` assignment of Group[1] past the position where it was first
encountered as a `$ref`.

**Hypothesis for root cause** (not 100% proven): another exception
during serialization (the `Qualifier.QualifiedTeam` getter throwing
on a greedy 3rd-place allocation failure — see #6) was aborting the
write mid-graph, leaving STJ's reference handler in an inconsistent
state. After fixing the throw (commit 9b870fc), the corruption stopped
reproducing.

**Cannot fully reproduce in unit tests.** Two repro tests were added in
`LocalStorageSerializationTests`:
- factory-output → serialize → deserialize round-trip (passes)
- serialize → deserialize → re-serialize → deserialize round-trip
  (passes)

The Web flow doesn't diverge from these in any way we've identified.
If the corruption ever recurs:
- `LocalStorageRepository.LoadBucket` quarantines the bad blob to a
  `fantasy-football:Competition:corrupt-{yyyyMMddHHmmss}` LocalStorage
  key and logs to the browser console. **Grab that blob next time it
  happens** — it's the only diagnostic we have for the underlying
  Web-specific behaviour.

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

### C. Tomorrow's next-up — merge Competitions + Statistics

The single remaining PR 1 item. Scope:
- Single `/competitions` page with Active / Finished tabs.
- Statistics inline or as a side panel.
- Drop `/statistics` from `NavMenu`.

Once that lands, PR 1 is done and can merge to `develop`. Then PR 2
(Identity) starts.

## Diagnostic affordances we added

These are in the tree and useful if the JSON corruption (#3) ever
recurs:

- **`LocalStorageRepository.LoadBucket` quarantine** (777d821) — on
  any `JsonException`, the corrupt blob is moved to a
  `fantasy-football:{Type}:corrupt-{yyyyMMddHHmmss}` key, the bucket
  starts empty, and the browser console logs blob length + error
  message. Reset Database (Settings) clears both.
- **`LocalStorageSerializationTests`** (3793d59, 0907827) — repro
  scaffold using the actual factory output (not SQLite-rehydrated).
  Currently green; if the Web ever produces JSON we can't, copying
  the corrupt blob and running it through this test is the path to a
  failing fixture.

## What I'm NOT confident about

- Whether the `MetadataReferenceNotFound` corruption (#3) is fully
  closed or just hidden behind the absence of the `QualifiedTeam`
  throw. The unit tests can't reproduce it, and we never proved the
  causal chain conclusively. If it recurs, the quarantine + console
  log will give us the smoking gun.
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

Next coding task: merge Competitions + Statistics (PR 1 § task C
above). After that lands, open `feature/ui-polish-1-ia-speed` →
`develop` PR.
