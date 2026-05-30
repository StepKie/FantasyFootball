# Data model + sources research — Venues, Players, Clubs, Leagues

Background: FantasyFootball ships 0.4.0 with national-team Elo data + xG + penalty
shootouts. The next direction is extending the Blazor app with Venues, Players,
Clubs, and league / Champions League competitions (issue #20 active focus).
This doc captures the data-source and data-model research done before
implementing — it should answer "where does our data come from" and "what does
the schema look like" before any code lands.

## Data-source recommendation

Three sources cover ~95% of what we need:

| Source | What we get | Format | Cost / friction |
|---|---|---|---|
| **openfootball** (github.com/openfootball) | National-team squads (WC/EM rosters), top-flight fixtures, baseline club + stadium metadata. Public domain. | Plain JSON + .txt files in many repos (`worldcup.json`, `clubs`, `stadiums`, `leagues`) | None. `git submodule` or just bundle the JSON files. No key, no rate limit, no licensing concerns. |
| **ClubElo** (clubelo.com/Data) | Club Elo ratings — the analog of eloratings.net's `World.tsv` we already use for national teams. | CSV. Three endpoints: `/YYYY-MM-DD` (full ranking on date), `/<CLUB_SLUG>` (history per club), `/Fixtures` (upcoming with calculated probabilities). | None. `curl`-able. Slug-based lookup (e.g. `ManCity`, `BayernMunich`) — fetch a date endpoint once to seed the slug table. |
| **transfermarkt-datasets** (github.com/dcaribou/transfermarkt-datasets) | Current club squads with positions, ages, market values (decent strength proxy). 37k+ players, 400+ clubs, 79k+ historical games, 1.8M+ appearances. | CSV / Parquet / DuckDB, weekly snapshot, CC0 license. | None. The maintainer scrapes Transfermarkt; we consume the CC0 dump (clean way). |

**Fallback / situational:**

- **football-data.org** — 10 req/min free with API key, covers PL/La Liga/Bundesliga/Serie A/Ligue 1/Eredivisie/UCL/WC/Euros. Use for live-season fixtures if openfootball is stale for the current matchday.
- **Wikidata SPARQL** — venue capacity + coordinates when openfootball/stadiums has gaps. Strong for top-flight grounds, uneven for lower divisions.

**Avoid as primary sources:**

- API-Football (api-sports.io) — 100 req/day free tier blows up on any modest fetch loop.
- Sportmonks free tier — only Danish Superliga + Scottish Premiership; useless for our scope.
- Live Transfermarkt scraping — Cloudflare bot detection + ToS friction. Use the CC0 dump instead.
- StatsBomb open data — event-level (per-pass, per-shot xG), overkill for our use case. Bookmark for a future xG-driven match engine.
- Football Manager (FM) data dumps — the FUTEK FM24 export (`futek.io/fmdb`, 474k players × 371 attributes) is available, but the modeling depth FM optimizes for (CA/PA scalars, ~40 visible attributes, hidden personality traits, Position/Role/Duty stack) is overkill for a tournament simulator. Bookmark if we ever add multi-season player development.

## Logo + asset sources

Three asset categories needed, three different sources:

| Category | Source | Format | Count | Size |
|---|---|---|---|---|
| **Confederation marks** (FIFA + 6 confederations) | **Wikimedia Commons** SVGs — hand-pick once. `File:FIFA_logo_without_slogan.svg`, `File:UEFA.svg`, `File:Conmebol_text_logo_2021.svg`, `File:Concacaf_logo.svg`, `File:Confédération_Africaine_de_Football_(logo).svg`, `File:Asian_Football_Confederation_emblem.svg`, `File:OFC_logo.svg` | SVG | 7 | ~140 KB |
| **Country flags** | **lipis/flag-icons** on GitHub — MIT-licensed, drop-in. `flags/4x3/` is the 250-SVG set keyed by ISO 3166-1 alpha-2 codes. | SVG | ~250 | ~1 MB |
| **Club crests** | **luukhopman/football-logos** on GitHub — all teams in top 25 European leagues, season-versioned, PNG 139×181 transparent. Active maintenance, annual refresh. | PNG 139×181 | ~500 (top-5 + UCL) | ~6-10 MB |

**Bundle strategy:**

- Confed marks + country flags → ship in the initial WASM payload (`wwwroot/img/confederations/` + `wwwroot/img/flags/`). ~1 MB total, negligible first-load impact.
- Club crests → ship as plain static assets, lazy-loaded via `<img src="img/clubs/{league}/{slug}.png">`. Browser caches; not in the initial bundle. The ~6-10 MB hits incrementally as users open competition pages, not all at once.

**Fallbacks** when a club is missing from luukhopman (rare for top-tier):
- **fclogo.top** — SVG quality, hundreds of clubs, but no bulk-download API (would need a per-club scrape).
- **TheSportsDB** — REST API at `r2.thesportsdb.com/images/media/team/badge/{hash}.png`, free with key `123`, 30 req/min.
- **Sortitoutsi FM24 pack** locally — most complete (~30k clubs in TCM24 at 512px) but FM-internal IDs need remapping to our slugs. Worth keeping as the absolute fallback.

**Refresh script:** add a `tools/fetch-logos.sh` analog to `tools/refresh-elo.awk` that re-pulls luukhopman + the confed SVGs annually. Same pattern as the Elo refresh — pinned source URLs, deterministic output, runs locally.

## Lessons from BasketBall-GM-Rosters (alexnoob/BasketBall-GM-Rosters)

The reference user mentioned. Useful shape choices we can adopt or adapt:

**Translate well:**

- **Per-entity history arrays.** One `Player` carries his career as `ratings[]`, `stats[]`, `awards[]`, `transactions[]` per season — not duplicated rows per season. Cleanest model when you want "view Pogba's 2018 → 2026 trajectory" without joins.
- **Transactions as the source of truth for team membership.** Player.transactions head = current club. Mid-season transfer is just another transaction entry; no "current_team_id" + separate transfers table that can drift.
- **Sentinel IDs.** `tid: -1` = free agent, `tid: -2` = retired. Avoids nullable foreign keys at the model level.
- **Sparse override files.** `team data.json` only stores per-team logo/color overrides — the engine fills the rest from defaults. Adapt as a thin `clubs.json` that overlays the openfootball baseline.
- **Schema version integer at top-level.** Start with `version: 1`. Cheap insurance against the inevitable first breaking change.
- **`relatives[]` for football families.** Cheap to support, lots of footballing dynasties (Touré, Maldini, Hazard).

**Basketball-specific — do NOT copy:**

- **15-attribute flat rating vector.** Football needs FIFA's 6-attribute split (`pace`, `shooting`, `passing`, `dribbling`, `defending`, `physical`) for outfield plus a **separate goalkeeping branch** (`gkDiving`, `gkHandling`, `gkKicking`, `gkReflexes`, `gkPositioning`). GK doesn't share the outfield axes.
- **2-char position strings.** Football has more positions than fit 2 chars (`CB`, `LWB`, `CDM`, `CAM`, …). Proper enum with primary + secondary positions.
- **Draft picks, draft lottery, college, conferences/divisions.** Football has transfers (not drafts), youth clubs (not US college), and flat league tables with promotion/relegation (not conferences). Skip entirely.

**Adopt explicitly:**

- The schema-version integer (above)
- `imgURL` per player and per-team-per-season — lets us swap kits/logos historically without forking the entity

**Adopt with caveats:**

- Hand-editable JSON as a contributor on-ramp — but provide a small Blazor admin form longer-term; ctrl-F'ing a 20MB file doesn't scale to >1 contributor.
- Don't let engine defaults silently fill missing top-level sections (BBGM does — `teams[]` is often omitted from community files). Make `clubs` and `leagues` required, even if there's a baseline import.

## Proposed data model

Sketch — final shape decided during implementation. Aligned with the existing
`Competition` flat model, not the deleted graph model.

```csharp
// FantasyFootball.Core/Models/Venue.cs
public sealed record class Venue
{
    public required string Id { get; init; }           // slug, e.g. "metlife-stadium"
    public required string Name { get; init; }
    public required string City { get; init; }
    public required string CountryCode { get; init; }  // ISO-3 (matches Country.Code3)
    public int? Capacity { get; init; }
    public (double Lat, double Lon)? Coordinates { get; init; }
    public string? TenantClubId { get; init; }         // for club home grounds (null for shared / international venues)
}

// FantasyFootball.Core/Models/Club.cs
public sealed record class Club
{
    public required string Id { get; init; }            // slug, e.g. "manchester-city"
    public required string Name { get; init; }
    public required string ShortName { get; init; }     // 3-char, e.g. "MCI"
    public required string CountryCode { get; init; }
    public string? HomeVenueId { get; init; }           // → Venue.Id
    public string? LeagueId { get; init; }              // current-tier league
    public string? LogoUrl { get; init; }
    public (string Primary, string Secondary)? Colors { get; init; }
    public int? Founded { get; init; }
    public int? Elo { get; init; }                      // from ClubElo CSV
}

// FantasyFootball.Core/Models/League.cs
public sealed record class League
{
    public required string Id { get; init; }            // slug, e.g. "premier-league"
    public required string Name { get; init; }
    public string? CountryCode { get; init; }            // null for UEFA/FIFA continental
    public required int Tier { get; init; }              // 1 = top flight; UCL = special tier (-1?)
    public required CompetitionType Format { get; init; } // DOMESTIC_LEAGUE, CHAMPIONS_LEAGUE, etc.
}

// FantasyFootball.Core/Models/Player.cs (sketch — design more carefully when implementing)
public sealed record class Player
{
    public required string Id { get; init; }                // slug or numeric
    public required string Name { get; init; }
    public string? ShortName { get; init; }
    public required string CountryCode { get; init; }        // nationality
    public required Position PrimaryPosition { get; init; }
    public Position[] SecondaryPositions { get; init; } = [];
    public required int BirthYear { get; init; }
    public PlayerRatings[] Ratings { get; init; } = [];      // per season
    public PlayerTransaction[] Transactions { get; init; } = []; // transfers / retirements
    public string? ImgUrl { get; init; }
}

public sealed record class PlayerRatings(int Season, int Overall,
    int Pace, int Shooting, int Passing, int Dribbling, int Defending, int Physical,
    // GK branch — populated only for GKs; ignored otherwise
    int? GkDiving = null, int? GkHandling = null, int? GkKicking = null,
    int? GkReflexes = null, int? GkPositioning = null);

public sealed record class PlayerTransaction(int Season, TransactionType Type, string? ClubId);
// TransactionType: TRANSFER, LOAN, FREE_AGENT, RETIRED, etc.

public enum Position { GK, CB, LB, RB, LWB, RWB, CDM, CM, CAM, LM, RM, LW, RW, CF, ST }
```

**Where data goes:**

- Bundled JSON (in `FantasyFootball.Core/Resources/Data/`) — venues, clubs, leagues. Refresh via tools/scripts (extend the `tools/refresh-elo.awk` pattern).
- ClubElo ratings — refresh via a small script analogous to `tools/refresh-elo.awk`, output an `Elo` column on the `Club` JSON or a separate ratings CSV joined by club slug.
- Player data — bundled JSON from a one-time pull of the transfermarkt-datasets dump, filtered to the leagues we simulate. Re-pull yearly or per release.

**Relationship to existing `Team`:**

`Team` currently models national teams (one team per country, ID = `ShortName` like `"GER"`, has `Elo` from `fifa_elo_new.csv`). Two options:

1. **Keep `Team` for national, add `Club` separately.** Cleaner separation, mirrors the real-world distinction.
2. **Generalize `Team` to cover both.** Add a `Type` discriminator (already exists as `TeamType.NATIONAL_MEN` / `CLUB_MEN` etc.). Then `Player.TeamId` points at either.

Recommend **(1)** — separate `Club` entity. National team rosters are a special case (one player per nationality slot, no transfers between national teams within a season). Clubs have transfers, league affiliations, home venues — different shape, different lifecycle. The two share enough that a thin shared interface (`ITeam` with `Id`, `Name`, `Elo`) is cheap; conflating them at the entity level isn't worth it.

**Home advantage:**

When league/Champions-League games arrive (which DO have a true home venue), the `IScoreModel` interface needs a per-game home-advantage parameter. Documented in `EloScoreModel`'s xmldoc already (`<remarks/>` says "+100 to home team is eloratings.net's convention"). Wire it through `IScoreModel.Score*Game(homeId, awayId, isAtHome)` when this work lands; existing international-tournament callers pass `false`.

## Open decisions (need user input before coding)

1. **Player rating system.** FIFA's 6-attribute (pace/shooting/passing/dribbling/defending/physical) is concrete and well-known, but each rating is then 1-99 — a lot of authoring. Alternative: a single 1-100 "overall" with position-dependent breakdowns derived. Or skip ratings entirely at the data-model stage and bolt them on when we implement player attribution. **Recommend: bolt on later — Phase 1 player model is just identity + position + team affiliation.**
2. **Player history depth.** BBGM keeps `ratings[]` per season; we could too, OR we could keep a single current-season rating and treat history as a future concern. **Recommend: single current rating to start, add history when we add a "career" feature.**
3. **Bundled vs runtime data.** Bundle as JSON at compile time (like `fifa_elo_new.csv` today), or fetch from APIs at runtime? **Recommend: bundle for offline-first.** A "refresh" button on Settings can pull from openfootball / ClubElo on demand for the user who wants newer data.
4. **Schema version.** Start `version: 1` on each new entity-JSON file? **Recommend: yes** — cheap insurance.

## Proposed phase / ticket breakdown

Smallest-to-largest, each phase is a self-contained PR:

### Phase 1: Venues

- **#NEW: Add `Venue` entity + WC 2026 host venue data.** Bundle a `venues.json` covering the 16 WC 2026 host stadiums (hand-curated from openfootball/stadiums + Wikidata). Add `Game.VenueId`. Surface as a chip on the scoreboard row.
- **#NEW: Venue detail popover from game-details.** Capacity, city, prior games at this venue in the current competition. Reuses the existing popover pattern.

### Phase 2: Clubs + Leagues (data scaffolding, no gameplay yet)

- **#NEW: Add `Club` and `League` entities.** Bundle `clubs.json` from openfootball/clubs + `leagues.json` for top-5 European + UCL. Build a small script in `tools/` that pulls ClubElo ratings into the bundled JSON, analogous to `tools/refresh-elo.awk`.
- **#NEW: Wire club logos.** Sortitoutsi.net FM24 pack assets (already noted as available in `docs/ideas.md`). FM-ID-to-club-slug mapping table.

### Phase 3: Players (data model only)

- **#NEW: Add `Player` entity with position + nationality + current club.** Bundle a subset from transfermarkt-datasets — top-5 European leagues + UCL participants, recent season. ~5000 players (manageable JSON size).
- **#NEW: Player listing on team detail page.** Squad table per club. No gameplay attribution yet (no scorer on Goal, no minutes played).

### Phase 4: League / Champions League gameplay

- **#NEW: Round-robin season factory.** Generic factory for double-round-robin domestic leagues. Generates fixtures, schedules across matchdays, persists as a `Competition`.
- **#NEW: Champions League factory.** Group stage → KO, similar to WC but with two-leg KO (home + away aggregate).
- **#NEW: Home advantage threading through `IScoreModel`.** Per-competition or per-game flag. Default off (existing international tournaments); on for the new league competitions.
- **#NEW: League table UI.** Different layout from cup standings — full-season table with position movement, GD column, points-per-game.

### Phase 5 (later): Match-event model, player attribution

Out of scope for now (issue #20 wishlist). Once Players exist as entities, the door is open for goal-scorer attribution → minute-stamped events → richer match timelines. Each step independently shippable.

## Sources

- Football data APIs: openfootball, ClubElo, transfermarkt-datasets, football-data.org, Wikidata SPARQL
- BBGM-Rosters: github.com/alexnoob/BasketBall-GM-Rosters, [Basketball-GM league schema](https://play.basketball-gm.com/files/league-schema.json)
