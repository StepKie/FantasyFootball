# UI/UX Polish Plan

A pre-deployment quality pass on the Blazor web host. Phase 4 (GitHub Pages,
issue #8) is gated on this work — we don't want the first public URL to look
like a Material default with a giant `Fantasy Football` H1 on every page.

## Reference apps

Universal patterns lifted from OneFootball, FotMob, Flashscore, Sofascore,
UEFA.com, BBC Sport, ESPN. Detailed synthesis in the conversation log; this
doc captures only the directives we're acting on.

---

## 1. Design ideas

### Brand & identity

- **Favicon + wordmark** — replace the default `@` favicon with a real mark.
  One-line wordmark in the top app bar (e.g. `⚽ FantasyFootball`).
- **Move off MudBlazor default purple.** Single accent at ≤10% usage; greys
  for everything else. Suggested neutrals: `#1A1A1A` text, `#6B6B6B` muted,
  `#E5E5E5` borders, `#F5F5F5` row stripe, `#FFFFFF` surface. Accent: a
  saturated competition green (`#00B86B` or similar) for CTAs only.
- **Typography**: keep a clean sans (Inter / system) for body. Add a
  condensed display face for tournament/round titles and scores —
  Barlow Condensed 700 or Oswald (free; web-friendly).
- **Tabular numerics globally** on score lines and table cells:
  `font-variant-numeric: tabular-nums` so `1-0` and `11-10` align.

### Information architecture

- **Kill the Home page.** Route `/` → `/competitions`. The current
  home-page text is dead weight.
- **Kill the giant H1 on every page.** Nav already tells you where you are.
  Where context matters, use a small breadcrumb-style line
  (`Competitions › WC 2026 #1`).
- **Merge Competitions + Statistics** into a single page with Active /
  Finished tabs. Per-row stats inline or in a side panel. Drops one nav
  entry.
- **About page**, reachable via a `?` icon in the top app bar. Contains:
  - One-line app description.
  - Keyboard shortcut reference (see Functionality § Keyboard navigation).
  - Build version / link to issue tracker.
- **Constrain picker widths.** Half-page-wide dropdowns are the strongest
  "untouched defaults" signal in the screenshots. Type + year on one row,
  both narrower.
- **`Start new` button** → tiny `+` icon button, or a split-button on the
  competition-type picker. Drop the two-line wrapping pill.
- **Standings column headers**: drop `Team` (obvious), consider dropping
  `#` or merging it into the team cell as a dim leading number.
- **Delete competitions from the list, not the detail page.** Move the
  trash icon off `/competitions/{id}` (where it sits next to back-nav and
  is dangerously easy to mis-click) into a per-row delete on the
  `/competitions` Active and Finished tables, with a confirmation dialog.

### Flag rendering

- Extract a shared `<FlagIcon Team Code Elo />` Razor component. Replace
  every inline `<img src="@FlagHelper.Url(code)">` with it.
- Three sizes: **16px** (inline / table rows), **24px** (match cards),
  **40px** (headers / hero).
- Configurable `FlagStyle` setting (persisted, single source of truth):
  - `Square` — current behaviour, kept for users who prefer it.
  - `Round` — circular via `clip-path: circle()` + `object-fit: cover`,
    1px inset border at `#0000001A` on light surfaces, none on dark.
  - `RoundTier` — circular + Elo-tiered border colour:
    - **Gold** Elo ≥ 1700 — `#C9A227` (muted, not brash `#FFD700`)
    - **Silver** Elo ≥ 1500 — `#9CA3AF`
    - **Bronze** Elo ≥ 1300 — `#B8732E`
    - **None** Elo < 1300 — subtle neutral border
- Settings page exposes a 3-radio picker with a live preview row.

### Match cards (game rendering)

- **Symmetric scoreboard layout**: `[flag][team]  —  SCORE  —  [team][flag]`
  mirrored around the score. Winner bolded, loser at ~65% opacity.
  Row height ~56–64px, 12–16px vertical padding.
- Replace the current body-text "Czech Republic 2-1 South Africa" list
  with proper game cards with subtle backgrounds and visible delimiters
  between groups.
- **Day-grouping dividers.** When the games panel shows games spanning
  multiple `PlayedOn.Date` values, insert a subtle date label
  (e.g. `Mon 25 Jun`) above each day's first card. Tiny text, secondary
  colour, single-pixel divider below — visible structure without
  competing with the cards.
- **Single-line scoreboard stays.** Multi-line layouts in apps like
  OneFootball exist to pack live status / scorer ticker / links to match
  pages — we don't have that data, so the symmetric single-row scoreboard
  is the right shape. Confirmed deliberate choice; do not multi-line.
- **`GameListMode` setting: `OnePerRound` vs `AllOfStage`.** The round
  picker on `/competitions/{id}` forces Round 1 → Round 2 → Round 3
  clicking to see everything. Add an alternative mode where the entire
  current Stage renders in one scrollable list, with day-grouping
  dividers between matchdays (and group letters where the stage has
  groups). The round picker stays but switches to "scroll-to-round"
  behaviour in this mode. Default and persisted via Settings.
- **Round picker shows date ranges next to round name.** Currently
  `Sechzehntelfinale` / `Final` — no temporal context. kicker.de:
  `Achtelfinale (10.03. - 18.03.)`. Annotate each picker entry with the
  min/max `PlayedOn` of the round so users see when it happens before
  switching. Same treatment for the stage picker.
- **Sub-score for extra-time / penalty games.** Currently the Result
  string crams "5-4 a.e.t." into one line. Separate: main 90-minute
  score on top, smaller sub-score below (matches the kicker.de pattern
  — `5:4` then `3:2` under). Cleaner visual hierarchy on KO bracket
  rows that ran past full time.
- **Configurable pre-match detail** via a `GameRowDetail` setting:
  - `Minimal` (default, current behaviour) — team names + score only.
  - `Probabilities` — pre-match win % shown subtly greyed inline,
    e.g. `Brazil 62% — vs — 38% South Africa`. Score replaces the %
    once the game is played.
  - `EloAndProbabilities` — both Elo rating and win %.
  - Calculation: Elo expectation
    `E = 1 / (1 + 10^((Rb − Ra) / 400))`, HFA = 0 (neutral venues).
    Simple two-way split; draw modelling can layer in later.
  - Same probability util feeds the click-for-game-details popover
    (Functionality § Click-for-game-details).
- Hover/click → details (see Functionality § Click-for-game-details).

### Group cards

- Coloured header strip or letter-badge (A / B / …) instead of plain
  `Group A` text. Instant recognisability at a glance.

### Standings table

- **4px coloured left-edge bar** marks qualification zones (green =
  advances, red = eliminated, none = neutral). Don't tint whole rows.
- Zebra stripe at ~4% black.
- `P W D L GF GA GD Pts` canonical column order. `Pts` bold, right-aligned.
- (Future) last-5 form as 24px W/D/L circles with the letter inside —
  accessibility-driven, never rely on colour alone.
- Eliminated rows at reduced opacity once mathematically out.
- **Optional Qual % column** (toggleable setting `ShowQualifyingProbability`):
  Monte Carlo probability that the team finishes in a qualifying position
  in its group, given current standings + remaining games. ~2000 sims per
  group, recomputed when a game in the group resolves; cached per-round.
  Subtle grey text. Off by default to avoid spoiling for users who don't
  want it. Shares the per-game Elo expectation util with the game-row
  probabilities (Match cards § Configurable pre-match detail). See
  Functionality § Group qualifying probability.

### Motion

- Animate state changes, not navigation. No page-level transitions.
- **Score reveal**: count-up or fade `-:-` → final, ~250ms.
- **Standings row reorder**: FLIP technique, ~250ms ease-out, when a
  finished game changes the table.
- **Just-finished game highlight**: subtle background pulse on the game
  row + matching team rows in standings for ~1.5s post-sim.
- **Bracket line draw**: ~400ms when a tie resolves and a winner advances.
- **Tournament-finish celebration**: brief confetti burst on
  `Competition.IsFinished`.

### Mobile

- Out of scope for the first polish pass beyond "don't actively break".
  Defer responsive bracket layout, drawer-style standings, etc., until
  after desktop polish lands.

---

## 2. Functionality ideas

### Bug to fix (correctness, not polish)

- **Third-place match populates with winners, not losers.** Current
  `KO Qualifier` rule for the third-place game returns the semifinal
  winners; should return the losers. Discrete commit at the top of
  PR #1.

### Speed control on the competition page

- A `MudButtonGroup` next to the sim buttons:
  `🐢 Slow  ▶ Normal  🐇 Fast  ⚡ Instant`. Per-session override of the
  Settings default `SimulationSpeed`.
- `Instant` short-circuits `Task.Delay(GameDelay)` and gates the
  per-game `GameFinishedMessage` (batch UI update to once at end —
  otherwise we still pay 72 re-renders for a group stage).
- Implementation: make `Simulator.GameDelay` mutable, set before each
  sim call; add a `Quiet` flag to suppress per-game messaging.
- **Unify Settings UI with the detail picker.** Replace the 0–500 ms
  slider on `/settings` with the same Slow / Normal / Fast / Instant
  preset picker. Loses arbitrary-ms granularity, gains a single shared
  vocabulary — settings persists the global default, detail overrides
  per visit. Avoids the current clash where the slider's value
  (e.g. 75 ms) doesn't map to any of the four detail buckets.

### Keyboard navigation

Page-level `@onkeydown` handler. Intercept only when focus isn't on a
text input. Help overlay on `?`; the same table lives on the About page.

| Key | Action |
|---|---|
| `Space` | Simulate next Game |
| `R` / `Shift+Space` | Simulate Round |
| `S` | Simulate Stage |
| `T` | Simulate Tournament |
| `←` / `→` | Prev / next Round in current Stage |
| `↑` / `↓` | Prev / next Stage (Group ↔ KO) |
| `Ctrl+Z` / `U` | Undo last sim action |
| `Esc` | Back to /competitions |
| `?` | Keybinding help overlay |

### Undo (rewind)

In-memory snapshot stack on `CompetitionDetailViewModel`.

- Before each user-clicked sim action, `JsonSerializer.Serialize(Competition)`
  pushes onto `Stack<string>`.
- `Undo()` pops, deserializes, replaces `Competition`, and calls
  `_repo.Save(Competition)` so persisted state also rewinds.
- **Granularity matches the action**: simmed by Round → undo restores
  the pre-Round state, not per-game. Matches user intent.
- Stack capped at 50 entries (a 48-team WC simmed per-game won't hit it).
- Cleared on `Competition.IsFinished`, `DataResetMessage`, and `Dispose`.
- Button next to sim buttons, `Disabled="_undoStack.Count == 0"`,
  `Icons.Material.Filled.Undo`.
- **In-memory only** — doesn't survive page reload. Matches the user's
  framing "as long as it hasn't finished and persisted".

### Group qualifying probability

Monte Carlo prediction of each team's probability of advancing out of
its group, surfaced as an optional `Qual %` column in the group-stage
standings table.

- Setting `ShowQualifyingProbability` (default off).
- ~2000 iterations per group: for each remaining game, sample a result
  from the Elo-based expected goal distribution; tally qualifying teams
  using the standard tiebreakers (Pts → GD → GF → H2H).
- Compute lazily, cache per-round; invalidate when any game in the
  group resolves.
- Heavy compute lives in a Monte Carlo helper that's pure functions on a
  copy of the group state — never touches `_repo`.

### Click-for-game-details

`MudPopover` or side drawer on game-row click:
- Elo of both sides at sim time.
- Computed win / draw / loss probabilities.
- Head-to-head record within this competition.
- Link to team detail page.

### Replay (simulate again) on tournament finish

When `Competition.IsFinished`, render a `Simulate Again` CTA that creates
a new competition with the same teams and Elo snapshot. Natural pairing
with the per-competition Elo snapshot work in issue #19.

### KO bracket view

Replace the round-list-and-dropdown with a real bracket.

- **Single-elimination horizontal flow**: Round of 16 → QF → SF → Final
  left-to-right, trophy at the right edge.
- **Persistent**: past rounds stay visible to the left; future fills in
  as games resolve. Solves "previous KO round disappears" and
  "future rounds visible?" in one go.
- **Text placeholders** for unfilled future slots
  (`Winner of QF1`, `1A vs 2B`), not greyed team logos.
- Lines connect winners forward; draw on resolution with the bracket-line
  animation (above).
- CSS-grid: one column per round, row-spans calculated so finals centre.
- Mobile: vertical stack or horizontal-snap scroll (deferred).

### Future rounds: visible read-only, not selectable

Push-back on the "selectable upcoming rounds" idea — selecting an empty
round shows `-:-` everywhere, which is the current weirdness. Keep
upcoming rounds visible (greyed) but not selectable. The bracket view
makes this moot anyway.

---

## 3. Technologies

### Already in the stack (no new deps)

- **Blazor WebAssembly** (.NET 10) — host project `FantasyFootball.Web`.
- **MudBlazor** — primary component library. Selective per-component
  overrides via `Class=` / `Style=` and a custom palette in `MudThemeProvider`.
- **CommunityToolkit.Mvvm** — keep all VMs and `ObservableProperty`/
  `RelayCommand` patterns as-is.
- **`System.Text.Json`** with the existing `IgnoreAttributeTypeInfoResolver`
  — reused for undo serialisation.
- **LocalStorage repository** — unchanged; sim actions persist as today.
- **Razor components** for shared widgets (`FlagIcon`, `GameCard`,
  `BracketView`, `KeyboardHelp`).
- **`@onkeydown` + `tabindex`** on the page root for keyboard nav.
  JS interop only if MudBlazor blocks the event flow.

### New dependencies (small, free)

- **`circle-flags`** (npm/CDN-hosted SVG flag set) — circular flag assets
  used by the `<FlagIcon>` component. Pure SVG, no JS.
- **`canvas-confetti`** (~20KB) — tournament-finish celebration. Loaded
  on demand via JS interop; only pulled in when a competition finishes.
- **Barlow Condensed** or **Oswald** webfont — via Google Fonts or
  self-hosted. Used for tournament titles + scores. Replaces nothing,
  layers on top of the existing Roboto/system stack.

### Pure CSS techniques (no library)

- **Circular flags**: `clip-path: circle()` + `object-fit: cover` +
  1px inset border.
- **Bracket layout**: CSS Grid with calculated `grid-row` spans per round.
- **FLIP row reorder** for standings: First-Last-Invert-Play animation
  pattern. Implementable in ~30 lines of JS interop, or a small Blazor
  helper component.
- **Score count-up**: Razor + a `Timer` (or CSS-only via `@property` +
  `counter-reset` if we want to stay JS-free).
- **Tabular numerics**: single CSS rule, no library.

### What we're **not** adding

- No SignalR, no server-side anything — stays static-deployable to GitHub
  Pages.
- No Tailwind / Sass — MudBlazor + scoped CSS is enough.
- No state-management library (Fluxor etc.) — VMs already do this.
- No premium fonts (Druk etc.) — licensing.

---

## 4. PR sequence

Four PRs, in order. Each branches from `develop` and lands back to
`develop`. Cumulative; later PRs build on earlier ones.

### PR 1 — IA + speed

**Status**: in flight on branch `feature/ui-polish-1-ia-speed`. Nearly
all items shipped; one item still open. See `docs/failure-log.md` for
session-spanning context (bugs hit, what's fixed, what's left).

- [x] Bug: third-place match populates winners, not losers.
- [x] Kill Home page; `/` → `/competitions`.
- [x] Kill H1s on all pages; add breadcrumb context line where useful.
- [x] **Merge Competitions + Statistics** into one page with Active /
  Finished tabs.
- [x] About page reachable via `?` icon (placeholder content + keyboard
  help table that fills in once PR 3 lands).
- [x] Constrain picker widths; type + year on one row.
- [x] `Start new` → `+` icon button.
- [x] Standings column header nits (drop `Team`, possibly `#`).
- [x] Speed control next to sim buttons (Slow / Normal / Fast / Instant);
  per-session override of the Settings default `SimulationSpeed`.

**Out-of-scope fixes that landed during PR 1** (bugs found while testing
the IA pass; see failure-log for detail): third-place placeholder
"Winner" vs "Loser", premature 3rd-place resolve, post-create stale
list, Instant-mode UI freeze, `MetadataReferenceNotFound` JSON
corruption, `[Ignore]` on `Qualifier.QualifiedTeam`, defensive try/catch
around third-place resolve, corrupt-blob quarantine on load.

### PR 2 — Identity

**Status**: in flight on branch `feature/ui-polish-2-identity`.

- [ ] Favicon (asset still to source/design).
- [x] Wordmark in top app bar (Barlow Condensed bold uppercase + ball glyph).
- [x] Primary colour off purple → green accent (`#00B86B`).
- [x] Typography: Inter body + Barlow Condensed display; tabular numerics
  globally on `:root`.
- [x] `<FlagIcon>` component (Team / Code / Size / Elo) with `FlagStyle`
  setting (`Square` / `Round` / `RoundTier`) wired to Settings.
- [x] Scoreboard-style game cards (symmetric, winner-bolded, loser dimmed,
  score in Barlow Condensed). Configurable pre-match detail
  (`GameRowDetail`) deferred to PR 3.
- [x] Group letter heading (typographic OneFootball-style, no chip, no
  noun).
- [x] Elo number tone-down on Teams + TeamDetail.

### PR 3 — Sim feel

**Status**: merged via PR #29 (2026-05-19). Branch
`feature/ui-polish-3-sim-feel` was rebased into 6 themed commits
(`a40310a..70b6127`) plus 5 auto-review rounds, then merged to
`develop` and re-released to `main` via PR #31.

Done:

- [x] Speed control (Slow / Normal / Fast / Instant) on competition page.
  (Already shipped in PR 1; left here for plan completeness.)
- [x] **Unify Settings speed UI with the detail picker** — replace the
  0–500 ms slider with the same 4-preset picker.
- [x] **Delete competitions from the list, not the detail page** — move
  the trash icon off `/competitions/{id}` to per-row delete on
  `/competitions` with a confirm dialog.
- [x] **Day-grouping dividers in the games panel** when `PlayedOn.Date`
  varies (`ddd d MMM` subtle separator).
- [x] **Group letter + kickoff time on scoreboard rows** (vertical tag
  on the leading side of each match-card row; letter blank for KO games
  whose teams span groups, kickoff time always shown).
- [x] **Standings qualifier highlight** (left-edge bar) — slim 3px
  muted-green / muted-red column inset from row edges; resolves
  ThirdPlace combination eligibility once the group stage finishes.
- [x] **Keyboard navigation** — document-level handler, full mapping,
  help overlay (`?`), About-page reference.
- [x] **Undo stack** — in-memory, per-game snapshot, capped at 50; UI
  attaches the Undo button to the row of the just-simmed game.
  Speculative undo pop moved into a `finally` block so a simulator
  exception can't leave a stale entry pointing at a SCHEDULED game.
- [x] **Just-finished game highlight** — single-game sim and Redo
  trigger a 1.5s green pulse on the row. Standings-row pulse deferred
  to a follow-up (see § PR 4 successors).

Still open — candidates for a future PR (PR 3.5 polish round, or
folded into PR 4):

- [ ] Score reveal animation (count-up or fade `-:-` → final).
- [ ] Standings row reorder via FLIP technique.
- [ ] Optional `Qual %` column in group stage (Monte Carlo).
- [ ] Click-for-game-details popover / drawer.
- [ ] Replay button on `Competition.IsFinished`.
- [ ] Confetti burst on finish.

Open visual / correctness questions:

- Group standings GD/Pts ordering anomaly — tracked in issue #33.
  Either a tie-break issue in `Standings.CreateFrom`, a stale-snapshot
  redraw, or a display-only bug.
- Per-row delete confirmation dialog text wording: revisit once the
  About page mentions the action.

### PR 4 — KO bracket (deferred, maybe never)

**Status**: deferred indefinitely (2026-05-20). The bracket-view
visualization may be added later as a polish item; treat the
round-list + dropdown as the canonical KO-stage UI for now.

Original scope, preserved for if/when this comes back:

- `<BracketView>` Razor component, CSS-grid based.
- Replaces the round-list-and-dropdown on KO stages.
- Persistent past rounds; text placeholders for unfilled future slots.
- Bracket-line draw animation on resolution.
- Group stage retains the current games + standings layout (bracket is
  KO-only).
