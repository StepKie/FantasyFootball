## [0.8.0](https://github.com/StepKie/FantasyFootball/releases/tag/0.8.0) — 2026-06-09

### New Features

- **Run as many simulations as you want** ([PR #75](https://github.com/StepKie/FantasyFootball/pull/75)) — bulk-simulating lots of competitions used to crash once the browser's small storage quota filled up (roughly a few dozen leagues). Saved competitions now use the browser's larger database instead, so you can run and keep far more — and the competitions list still loads fast with hundreds saved.
- **Bottom play button on phones** ([PR #76](https://github.com/StepKie/FantasyFootball/pull/76)) — a thumb-reachable button at the bottom of the screen drives the main action on each page: start a new competition, play the next game, or replay a finished one. Desktop keeps the inline buttons and the Space shortcut.

### Bugfixes

- **Faster game-details popover and standings with many saved competitions** ([PR #76](https://github.com/StepKie/FantasyFootball/pull/76)) — the head-to-head stat and the overall-standings panel no longer re-scan every saved competition on each open, and the popover opens and closes without lag.
- **CIS (Euro 1992) flag shows in overall standings** ([PR #75](https://github.com/StepKie/FantasyFootball/pull/75)) — it no longer falls back to a missing club crest.

## [0.7.1](https://github.com/StepKie/FantasyFootball/releases/tag/0.7.1) — 2026-06-08

### Bugfixes

- **Round-of-32 duplicates closed** ([PR #73](https://github.com/StepKie/FantasyFootball/pull/73)) — group-position knockout slots (Group A runner-up, Group B runner-up, …) could lock to mid-group standings the moment the first group game scored, and never refreshed as more games played. When the third-place pool later resolved against final standings, the same team could end up in both a group-position slot and a pool slot. Slots now track live standings and only freeze when their game is actually played; as a bonus, the Round-of-32 bracket now shows live predicted matchups that update as group standings shift.
- **Round of 32 fills in when you reopen a competition mid-tournament** ([PR #73](https://github.com/StepKie/FantasyFootball/pull/73)) — closing the page on the last group game and reopening used to show qualifier placeholders ("Group A runner-up", "Best 3rd of A/B/C/D/F") instead of resolved team names. The bracket now resolves on load.
- **Played-game result no longer hides behind the title bar on phone** ([PR #73](https://github.com/StepKie/FantasyFootball/pull/73)) — playing a game now centers the just-finished row in view with context above and below, instead of sliding it under the sticky app bar.
- **End-of-round no longer yanks the bracket forward before you can read the result** ([PR #73](https://github.com/StepKie/FantasyFootball/pull/73)) — playing the last game of a round used to scroll the page to the next round about a second later, taking the just-finished result off-screen. The view now stays on the result.

## [0.7.0](https://github.com/StepKie/FantasyFootball/releases/tag/0.7.0) — 2026-06-08

### New Features

- **Phone-friendly layout** ([PR #71](https://github.com/StepKie/FantasyFootball/pull/71)) — game cards, standings, the competitions list and detail headers all reflow for narrow screens. Game rows stay on one line; long club names like "Borussia Mönchengladbach" use a compact form ("Gladbach") on small cards and break at curated points when the full name still has to fit. Standings tables stay real tables on phones instead of collapsing to stacked label/value rows, and the competitions list shows one row per competition.

### Bugfixes

- **Third-place qualifiers no longer duplicate across knockout slots** ([PR #69](https://github.com/StepKie/FantasyFootball/pull/69)) — in WC48-style formats with overlapping third-place pools, the previous resolver could assign the same team to two Round-of-32 slots and silently corrupt the bracket. Slots now fill via a true matching: every slot gets a team whenever a valid assignment exists, and the qualifiers are exactly the best-ranked teams. An unfillable slot fails loudly instead of silently.

## [0.6.0](https://github.com/StepKie/FantasyFootball/releases/tag/0.6.0) — 2026-06-02

### New Features

- **Bundesliga 2025-26** ([PR #65](https://github.com/StepKie/FantasyFootball/pull/65)) — simulate the German top flight with real fixtures, real results so far, and club crests for all 18 teams. Domestic-league competitions are a new format: season-long round-robin, no group stage, no knockout. League standings show qualification bands (Champions League, Europa League, Conference League, relegation playoff, relegation) per position.
- **Premier League, Serie A, LaLiga, Ligue 1 (2025-26)** ([PR #66](https://github.com/StepKie/FantasyFootball/pull/66)) — the same treatment for four more top-flight leagues. Real fixtures and results, 78 additional club crests, 96 club Elo ratings from a single snapshot.
- **Pick the Elo set per competition** ([PR #65](https://github.com/StepKie/FantasyFootball/pull/65)) — the rating set used to simulate a competition is now chosen at competition setup instead of being driven by a single global "active" set. National-team and club rating snapshots are kept separate, so the picker only shows compatible options.
- **Per-team rating history on the Teams page** ([PR #65](https://github.com/StepKie/FantasyFootball/pull/65)) — opening a team now shows a table of every stored rating snapshot that covers it (date, rank, Elo), instead of a single value.
- **"Restore historical competitions" button in Settings** ([PR #65](https://github.com/StepKie/FantasyFootball/pull/65)) — explicit, idempotent restore of bundled World Cup / Euro tournaments after a database reset. Replaces the previous behaviour of implicitly seeding them on first visit to the Competitions page.

### Bugfixes

- **Bundled historical competitions simulate correctly after a reset** ([PR #65](https://github.com/StepKie/FantasyFootball/pull/65)) — restoring the historical World Cup / Euro tournaments used to leave them without a rating set assigned, which made any sim refuse to run. Each bundled tournament now ships with its rating set pinned to the matching year.

## [0.5.0](https://github.com/StepKie/FantasyFootball/releases/tag/0.5.0) — 2026-05-30

See the [compare view](https://github.com/StepKie/FantasyFootball/compare/0.4.0...0.5.0).

## [0.4.0](https://github.com/StepKie/FantasyFootball/releases/tag/0.4.0) — 2026-05-24

See the [compare view](https://github.com/StepKie/FantasyFootball/compare/0.3.1...0.4.0).

## [0.3.1](https://github.com/StepKie/FantasyFootball/releases/tag/0.3.1) — 2026-05-20

See the [compare view](https://github.com/StepKie/FantasyFootball/compare/0.3.0...0.3.1).

## [0.3.0](https://github.com/StepKie/FantasyFootball/releases/tag/0.3.0) — 2026-05-20

First tagged release.
