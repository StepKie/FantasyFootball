# Idea pile

Unsorted feature/UX ideas that aren't yet committed to a PR plan. Promote to
`ui-polish-plan.md` (or a dedicated planning doc) when an idea graduates to
"we're doing this next".

## Simulation model

- **Pluggable simulation models.** Today's sim is one Elo-based model with
  fixed parameters. Offer alternatives:
  - **Elo (current)** — baseline.
  - **Elo + outliers** — modified Elo with heavier tails (more upsets / blowouts).
  - **Tweakable parameters** — expose math knobs (Elo K-factor, goal-distribution
    variance, home-field advantage, draw bias) for an "Advanced" mode.
- UX shape: a `SimulationModel` setting (radio + per-model param panel) for
  power users. Default stays Elo with current params.

## Settings as overlay (not a separate page)

- Replace `/settings` page with a modal / dropdown / slide-over overlay
  triggered from the app bar.
- The page underneath stays visible → user sees the effect of a setting
  (flag style, game-row detail, sim speed) directly on the current page
  without navigating away and back.
- Mirrors how OneFootball / Sofascore handle preferences.

## Venue / location surface

- **Show venue on game cards** (e.g. where the kickoff time sits, or next to it).
  Configurable visibility so users who want a clean row can hide it.
- **A richer location surface** somewhere in the app — stadium, city,
  capacity, previous games played here in this competition, the home team
  if it's a club ground / national stadium, anything else venue-flavoured.
  Open question on shape:
  - Popover/drawer when clicking the venue chip on a game card?
  - Dedicated "Venues" tab on the competition page?
  - Venue page reachable from each game's details popover (see
    `ui-polish-plan.md § Click-for-game-details`)?
- Data work first: venue is currently a `Game.Location` string (or
  similar). For previous-games / capacity / coords we'd need a Venue
  entity with proper relationships. Probably a follow-up to whatever
  surface lands first.

## Logos: confederations, country flags, club crests

- **Confederation marks** (UEFA, CONMEBOL, AFC, CAF, CONCACAF, OFC, plus
  FIFA as the global parent) in the team picker, confed-scoped filters,
  and next to `/competitions` type pickers.
- **FIFA WC + UEFA Euros marks** next to the competition-type picker on
  `/competitions` (and on the start-new dialog), so the visual identity of
  the tournament is immediately recognisable.
- **Club crests** for the eventual league + Champions League work.
- Country flags already shipped (FlagIcon component), but worth migrating
  to the lipis/flag-icons SVG set for cleaner vector rendering.

**Sources** (see `docs/data-model-research.md § Logo + asset sources` for
the full table):

- Confed marks → Wikimedia Commons SVGs, ~7 files, ~140 KB total.
- Country flags → lipis/flag-icons on GitHub (MIT-licensed, 250 SVGs).
- Club crests → luukhopman/football-logos on GitHub (PNG 139×181, all
  teams in top 25 European leagues, season-versioned, annual refresh).
- Sortitoutsi FM24 pack (available locally) is the most complete fallback
  but uses FM-internal numeric IDs; only worth the remap work for clubs
  missing from luukhopman, which is rare for top-tier teams.

**Bundle strategy:** ship confed + flag SVGs in the initial WASM payload
(~1 MB, negligible). Lazy-load club crests via `<img src=...>`. Annual
`tools/fetch-logos.sh` refresh, same pattern as `tools/refresh-elo.awk`.

## Active focus (2026-05-24) — web-app extensions

Replaces the previously-planned MAUI Phase 5 (#9, paused).

### Current Elo data
- Pull recent FIFA / national-team ratings via a free public API (rather
  than the bundled CSV which is whatever-snapshot-was-handy). Refreshable
  from the app on demand, or via a build-time fetch.
- Historical national-team Elo lower priority — feasible options:
  scrape eloratings.net, recompute from `martj42/international_results`
  match history, or surface as a separate dataset later.
- See #30 for the current-Elo bundled-CSV refresh ticket; the live-pull
  is a step beyond that.

### Venues data model + UI surface
- `Venue` entity: city, stadium name, country, capacity, optional coords.
- Wire onto game cards (configurable visibility).
- Richer venue surface — popover from the game-details click, or
  dedicated Venues tab on the competition page, or venue page from
  game-details.

### Players (data model first)
- `Player` entity: rating, position, club / national team.
- No gameplay attribution yet — model is the unblocker for the later
  match-event work (scorer attribution, minute, card/sub data).
- Reference shape: BasketBall-GM-Rosters
  (`alexnoob/BasketBall-GM-Rosters`) ships custom JSON roster files;
  worth studying for how they keyed players to clubs and seasons.

### More leagues
- `CompetitionType.CHAMPIONS_LEAGUE` and `DOMESTIC_LEAGUE` enum values
  already exist; `HistoricalData.AvailableYears` throws
  `NotImplementedException` for both.
- Needs: round-robin season factories, club roster data
  (ClubElo's public CSV API: http://clubelo.com/Data), league-table UX
  distinct from cup standings.
- Club logos: bundle real logos from the same FM24 pack (or
  transfermarkt / similar).

## Manual competition setup — drawing-ceremony UX

Applies to the *manual* setup mode only (Classic / Random unchanged):

- **Drag-and-drop** team entries between group slots.
- **Double-tap** an empty slot to type with autocomplete (existing team
  search).
- **Pot-based animated draw**: emulate the official ceremony — teams sit in
  pots, click "draw" to animate one team at a time landing in the next
  legal group slot. Subtle confetti/ball-bounce; respects standard draw
  constraints (no two teams from same confederation in one group, etc.).
- Natural extension of the existing manual setup; differentiates the app
  vs "just pick teams from a dropdown".
