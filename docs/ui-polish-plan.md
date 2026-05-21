# UI/UX Polish Plan

Originally a pre-deployment quality pass on the Blazor web host (gated
Phase 4 / GitHub Pages, issue #8). **Phase 4 shipped; app is live at
`stepkie.github.io/FantasyFootball/`, currently 0.3.1.** Most of this
document's original scope is done — the sections below have been split
into Shipped / Open / Deferred. The design-rationale sections at the
bottom (§ Design ideas, § Functionality ideas, § Technologies) are
preserved as reference for items still in flight or future work.

---

## Shipped

The full pre-deployment plan plus several follow-ups landed across PRs
#1–#41. Highlights, grouped by theme:

### Brand & identity
- Wordmark in top app bar (Barlow Condensed + ball glyph); off-purple
  green accent (`#00B86B`); Inter body + condensed display face for
  scores/titles; tabular numerics globally.
- `<FlagIcon Team Code Elo Size>` component with `FlagStyle` setting
  (`Square` / `Round` / `RoundTier`) wired to Settings.

### Information architecture
- Home page killed; `/` → `/competitions`.
- Giant H1s killed; small breadcrumb-style context line where useful.
- Competitions + Statistics merged into one page with Active / Finished
  tabs.
- About page reachable via `?` icon in the top app bar (keyboard help +
  build version).
- Constrained picker widths; type + year on one row.
- `Start new` → `+` icon button.
- Per-row delete on `/competitions` (moved off the detail page, with
  confirm dialog).

### Match cards & games panel
- Symmetric scoreboard layout: `[flag][team] — SCORE — [team][flag]`
  with winner-bolded, loser dimmed, Barlow Condensed score, tabular
  numerics.
- Day-grouping dividers when the games panel spans multiple
  `PlayedOn.Date` values.
- Group letter + kickoff time tag on each row.
- **Hierarchical chip-strip pickers (PR #36):** Stage chips above,
  Round chips below, dashed-line backdrop, status encoded via fill +
  border + opacity. Replaces the original dropdown round picker.
- **Whole-stage scroll layout (PR #36):** all rounds in the selected
  stage render as scrollable sections, bounded by viewport.
- **Sim-round button moves between chips** (visible only where the
  round has games remaining).
- **Jump-to-current 📍 pin** scrolls to the actual current-game row.
- **Click-for-game-details popover** on scoreboard rows: Elo of both
  sides, pre-match win/draw/loss probabilities, cross-competition H2H
  tally, link to team detail.

### Standings table
- Left-edge qualification-bar (green = advances, red = eliminated,
  none = neutral); resolved on group-stage finish, including
  third-place combination eligibility for the expanded WC format.
- Group-letter heading (no chip, no noun) on each group block.
- Standings panel bounded by viewport with internal scroll; pickers
  stay pinned.

### Sim experience
- Speed control (Slow / Normal / Fast / Instant) on the competition
  page, unified with the Settings preset picker (no more 0–500ms
  slider mismatch).
- **Score reveal animation:** brand-green tinted pulse on the row at
  the moment the final score appears (single-render `FlashRecentlyFinished`
  ensures click + Space paths both pulse).
- **Undo stack** (per-game, capped at 50, in-memory) with Undo +
  Redo buttons attached to the just-simmed game row.
- **Confetti burst** on `Competition.IsFinished` (canvas-confetti via
  JS interop, loaded on demand, guarded against load-race re-fires).
- **Replay** ("Simulate Again") CTA on tournament finish — clones the
  team lineup into a fresh competition and navigates.

### Keyboard navigation
- Document-level handler (intercepts only when focus isn't on a text
  input); help overlay on `?`; same table on the About page.
- Mapping: Space (game), R / Shift+Space (round), S (stage), T
  (tournament), ←/→ (round), ↑/↓ (stage), Ctrl+Z / U (undo), Esc
  (back to /competitions), ? (help).

### Infrastructure
- **GitHub Pages deploy** via `actions/deploy-pages` (Phase 4 of #10).
- **Serilog → BrowserConsole sink** wired in `Program.cs` so
  `Log.Debug` / `Log.Information` reach DevTools (gated `MinimumLevel.Is`
  Debug under `#if DEBUG`, Warning in Release).
- **WC 2026 expanded format** (12 groups, 48 teams) including
  backtracking-based third-place qualifier resolution.
- **GroupView qualifier-walk cache** (perf; the dominant cost during
  KO sim per the original observation in #34).
- **SQLite multi-OneToOne FK stomp workaround** (PR #41) so KO games
  survive Repo.Save + Repo.Get round-trips intact.

---

## Open — items with UI visibility

These are concrete, scoped, and still worth doing. Sorted roughly by
leverage:

- [ ] **Bulk simulate ×N from setup (#37).** A "Sim count" input on
  the setup page; runs N tournaments in memory (no per-comp
  persistence — store only the aggregate result); shows a
  winner-frequency table / distribution chart. Turns the app from
  "watch one tournament" into "explore probabilities." Highest
  user-visible leverage left.
- [ ] **Settings as overlay** (from `docs/ideas.md`). Replace the
  `/settings` page with a slide-over from the app bar so flag-style /
  sim-speed changes apply live to the page underneath. Mirrors the
  OneFootball / Sofascore pattern. Contained ~half-day scope.
- [ ] **Confederation + competition logos on pickers** (from
  `docs/ideas.md`). FIFA WC + UEFA Euros marks next to the
  competition-type picker; confederation logos next to confed-scoped
  filters. Blocked on licensing: the sortitoutsi.net FM24 pack
  available locally is community-aggregated and not safe to
  redistribute on a public GitHub Pages deploy — needs SVG
  equivalents or a separate static origin.
- [ ] **Pluggable simulation models** (from `docs/ideas.md`). A
  `SimulationModel` setting (Elo / Elo+outliers / Tweakable
  parameters), radio + per-model param panel for power users.
  Default stays current Elo+Poisson. Larger design surface.
- [ ] **Manual competition setup — drawing-ceremony UX** (from
  `docs/ideas.md`). Drag-and-drop between group slots, double-tap to
  autocomplete an empty slot, pot-based animated draw emulating the
  official ceremony. Applies to manual setup mode only; Classic /
  Random unchanged.
- [ ] **Venue / location surface** (from `docs/ideas.md`). Venue chip
  on game cards (toggleable); richer venue page reachable from the
  game-details popover (stadium, city, capacity, prior games here).
  Needs a `Venue` entity first — current `Game.Location` is a string.
- [ ] **FLIP standings row reorder.** When a game resolution changes
  the table, animate row reshuffle (~250ms ease-out) instead of the
  current instant jump.
- [ ] **Optional `Qual %` column in group stage** (Monte Carlo
  qualification probability). Setting `ShowQualifyingProbability`
  (default off); ~2000 sims per group, cached per-round, recomputed
  on game resolution. Shares the Elo expectation util with the
  game-row probabilities already in click-for-game-details.
- [ ] **Round / stage picker date ranges** — annotate each chip with
  the min/max `PlayedOn` of its round (kicker.de pattern:
  `Achtelfinale (10.03. – 18.03.)`).
- [ ] **Sub-score for extra-time / penalty games.** Currently the
  Result string crams `5-4 a.e.t.` into one line; separate the
  90-minute score (top) from the ET/pen sub-score (smaller, below).
- [ ] **Configurable pre-match game-row detail.** `GameRowDetail`
  setting: Minimal (default), Probabilities (subtle inline pre-match
  win % replacing the score until played), EloAndProbabilities.

## Open — backend correctness / perf with secondary UI visibility

- [ ] **WC 2026 third-place fallback (#12, narrowed).** Backtracking
  is correct given a top-8, but doesn't fall back to alternative
  8-subsets if the natural top-8 is mathematically unmatchable. Rare
  but visible as a TBD placeholder if it ever fires. Acceptance
  starts with a 10,000-sim empirical measurement.
- [ ] **Per-competition Elo snapshot (#19).** Replay currently uses
  today's Elo on each Team instance, not a snapshot from the finished
  comp. Removes a latent correctness wart in the replay flow.
- [ ] **Cache KoGame qualifier resolution (#42).** WC48 per-game sim
  cost is ~3× WC32/EM24 because `KoGame.HomeTeam` recomputes
  `Group.GetStandings()` on every access. Once the group stage is
  finished, the standings are immutable and cacheable. Visible as
  smoother KO-phase sim at Fast / Normal speed.
- [ ] **Retire `sqlite-net-pcl` (#44).** Tests are the only
  consumer; deployed app already uses LocalStorage. Replacing with
  an in-memory JSON repo collapses ~200 lines of workaround in
  `Repository.cs` and resolves #43. Test suite drops from 8+ min to
  seconds. No direct UI impact, but unlocks iteration loop velocity.
- [ ] **Update bundled FIFA Elo CSV (#30).** Small data refresh;
  affects every Elo number users see.

## Mobile

Still out of scope for the desktop polish pass beyond "don't actively
break." Responsive bracket layout, drawer-style standings, etc., remain
deferred until desktop polish converges.

## Deferred — maybe never

### KO bracket view

Still deferred (2026-05-20). The bracket-view visualisation may be
added later; treat the round-list + chip picker as the canonical
KO-stage UI for now. Original scope preserved in § Functionality
ideas → KO bracket view below.

---

## Design ideas (reference)

### Brand & identity

- **Favicon** — still pending an asset to source/design.
- **Wordmark, palette, typography, tabular numerics** — shipped, see
  § Shipped.

### Information architecture

- Most items shipped. Open items in § Open above.

### Flag rendering

- Three-mode `FlagIcon` (Square / Round / RoundTier) — shipped.
- Elo-tiered border thresholds: Gold ≥ 1700 (`#C9A227`), Silver ≥ 1500
  (`#9CA3AF`), Bronze ≥ 1300 (`#B8732E`), None < 1300.

### Match cards (game rendering)

- Symmetric scoreboard, day-grouping dividers, single-line layout —
  shipped.
- Round / stage picker date ranges — open.
- Sub-score for ET / penalty games — open.
- Configurable pre-match detail (`GameRowDetail`) — open.

### Group cards

- Letter-badge heading — shipped (typographic, no chip).

### Standings table

- 4px coloured left-edge qualifying bar — shipped.
- `P W D L GF GA GD Pts` canonical column order — shipped, with
  `Pts` bold, right-aligned.
- Zebra stripe at ~4% black — pending (could land with FLIP reorder).
- Last-5 form circles — future, accessibility-driven design.
- Eliminated rows at reduced opacity — pending.
- Optional `Qual %` column — open.

### Motion

- Score reveal — shipped.
- Just-finished row highlight — shipped (game row only; standings
  row pulse deferred).
- Confetti burst on finish — shipped.
- FLIP standings reorder — open.
- Bracket line draw — deferred with the bracket view.

### Mobile

- Out of scope, see § Mobile above.

---

## Functionality ideas (reference)

### Speed control

Shipped. The 0–500ms slider on `/settings` was replaced with the same
Slow / Normal / Fast / Instant preset picker as the detail page.

### Keyboard navigation

Shipped. Document-level handler, full mapping, help overlay (`?`),
About-page reference. Implementation lives in `CompetitionDetail.razor`.

### Undo (rewind)

Shipped. Per-game snapshot stack capped at 50, with Undo + Redo
buttons attached to the just-simmed game row. The
`JsonSerializer.Serialize(Competition)`-on-each-action plan was simplified
to `Game.ClearResult()` since the granularity is per-game.

### Group qualifying probability

Open — see § Open list above.

### Click-for-game-details

Shipped (PR #35). Elo, win/draw/loss probabilities, H2H tally,
link to team detail. Probabilities use the standard Elo expectation
formula `E = 1 / (1 + 10^((Rb − Ra) / 400))` with HFA = 0; same util
feeds future game-row probability mode.

### Replay (simulate again) on tournament finish

Shipped (PR #35). Clones the team lineup into a fresh competition
and navigates. Uses today's Elo on each Team — per-competition Elo
snapshot is a follow-up (#19).

### KO bracket view (deferred)

Original scope preserved for if/when this comes back:

- `<BracketView>` Razor component, CSS-grid based.
- Single-elimination horizontal flow: R16 → QF → SF → Final
  left-to-right, trophy at the right edge.
- Persistent past rounds; text placeholders for unfilled future slots.
- Lines connect winners forward; draw on resolution with the
  bracket-line animation.
- Group stage retains the current layout; bracket-view is KO-only.

### Future rounds: visible read-only, not selectable

Resolved by the chip-strip pickers (PR #36): future rounds render
visibly with a dashed border + 50% opacity, not selectable.

---

## Technologies

### In the stack

- **Blazor WebAssembly** (.NET 10) — host project `FantasyFootball.Web`.
- **MudBlazor** — primary component library; selective overrides via
  `Class=` / `Style=` and a custom palette.
- **CommunityToolkit.Mvvm** — VMs with `[ObservableProperty]` /
  `[RelayCommand]`.
- **`System.Text.Json`** with `IgnoreAttributeTypeInfoResolver` —
  drives the LocalStorage persistence path.
- **LocalStorage repository** — single source of truth for persisted
  user state in the deployed app.
- **`@onkeydown` + `tabindex`** on page root for keyboard nav.
- **`circle-flags`** — SVG flag set used by `<FlagIcon>`.
- **`canvas-confetti`** — loaded on demand, only when a competition
  finishes.
- **Inter + Barlow Condensed** webfonts via Google Fonts.
- **Serilog + Serilog.Sinks.BrowserConsole** — `Log.Debug` / `Log.Information`
  reach DevTools console.

### Still not adding

- No SignalR, no server-side anything — stays static-deployable to
  GitHub Pages.
- No Tailwind / Sass — MudBlazor + scoped CSS suffices.
- No state-management library — VMs already do this.
- No premium fonts.
