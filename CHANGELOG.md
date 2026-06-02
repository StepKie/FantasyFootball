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
