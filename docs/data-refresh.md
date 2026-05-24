# Refreshing the bundled FIFA Elo data

The simulator uses `src/FantasyFootball.Core/Resources/Data/fifa_elo_new.csv`
as the source of every national team's Elo rating. The file is an embedded
resource (`<EmbeddedResource>` in `FantasyFootball.Core.csproj`); replace
it on disk, rebuild, and every Team picks up the new rating. There is no
runtime fetch — refreshes are checked into git.

## Source

**eloratings.net** publishes their dataset as TSV at predictable URLs:

| URL | Contents |
|---|---|
| `https://www.eloratings.net/World.tsv` | Current ratings (updates daily after international matches) |
| `https://www.eloratings.net/{year}.tsv` | Year-end snapshot for that year (verified 1970 → 2024) |

Both files share the same schema. The site renders these in the browser
via JavaScript, so `WebFetch` returns a summarized view — use `curl`
directly to get raw bytes.

## TSV schema

Tab-separated, no header. The columns I rely on:

| Col | Meaning |
|---|---|
| 1 | Rank (ties shown as the same number; can be `−` in year files) |
| 3 | Two-letter team code (eloratings-internal, NOT ISO; see mapping below) |
| 4 | Current Elo rating (integer) |

Columns 5+ are 1-year / 5-year / 10-year deltas + match counts — not used
for the refresh.

## Eloratings code map → our CSV's `country_code_2`

Nine cases where eloratings' 2-letter code differs from ours:

| Eloratings | Ours (`country_code_2`) | Team |
|---|---|---|
| `EN` | `GB-ENG` | England |
| `SQ` | `GB-SCT` | Scotland (eloratings uses `SC` for Seychelles) |
| `WA` | `GB-WLS` | Wales |
| `EI` | `GB-NIR` | Northern Ireland |
| `NM` | `MK` | North Macedonia |
| `KO` | `XK` | Kosovo |
| `SW` | `SZ` | Eswatini |
| `TI` | `PF` | Tahiti |
| `VG` | `IO` | British Virgin Islands (our `IO` is mislabelled — the row is actually BVI) |

Every other row joins directly on the 2-letter code.

Eloratings carries ~244 teams (incl. non-FIFA / overseas territories);
our CSV is 211 (FIFA members + UK constituents + Kosovo). The
extra-eloratings entries (Greenland, Wallis & Futuna, Vatican, etc.)
are silently ignored.

## Procedure

1. **Download the source**
   ```bash
   curl -sfo /c/Temp/elo/world.tsv https://www.eloratings.net/World.tsv
   # OR for a historical snapshot:
   # curl -sfo /c/Temp/elo/2018.tsv https://www.eloratings.net/2018.tsv
   ```

2. **Sanity-check the join.** Eloratings sometimes adds/renames codes;
   verify nothing in our top ~50 fails to match before producing the
   output.
   ```bash
   awk -F'\t' '{print $3}' /c/Temp/elo/world.tsv | sort > /c/Temp/elo/elo_codes.txt
   awk -F, 'NR>1 {print $4}' src/FantasyFootball.Core/Resources/Data/fifa_elo_new.csv \
     | sort > /c/Temp/elo/our_codes.txt
   # Codes in eloratings but not in ours (after applying the map):
   comm -23 /c/Temp/elo/elo_codes.txt /c/Temp/elo/our_codes.txt
   ```
   The eight special cases above will show up here every run; anything
   else is a new code that needs investigating (most likely a country
   rename — look it up in `https://www.eloratings.net/en.teams.tsv`
   which lists `code\tEnglish name`).

3. **Produce the updated CSV** with `tools/refresh-elo.awk`:
   ```bash
   awk -f tools/refresh-elo.awk \
     /c/Temp/elo/world.tsv \
     src/FantasyFootball.Core/Resources/Data/fifa_elo_new.csv \
     > /c/Temp/elo/fifa_elo_NEW.csv
   ```
   The script updates `rank`, `elo_new`, `rank_date`. It leaves
   `elo_old`, `name`, `country_code_*`, `confederation`, and the
   German name untouched. Edit the `new_date` variable at the top
   to reflect the snapshot date.

4. **Side-by-side preview** (optional sanity check before overwriting):
   ```bash
   awk -F, '
     NR==FNR { if (FNR>1) old[$4] = $6; next }
     FNR>1 { d = $6 - old[$4]; printf "%4d  %-25s  %7.0f → %7s  %+5.0f\n", int($1+0), $2, old[$4], $6, d }
   ' src/FantasyFootball.Core/Resources/Data/fifa_elo_new.csv \
     /c/Temp/elo/fifa_elo_NEW.csv | sort -k1 -n | head -30
   ```

5. **Replace the file + run tests:**
   ```bash
   cp /c/Temp/elo/fifa_elo_NEW.csv src/FantasyFootball.Core/Resources/Data/fifa_elo_new.csv
   dotnet test src/FantasyFootball.Tests/FantasyFootball.Tests.csproj
   ```
   Tests should pass without changes — none of them assert specific Elo
   values.

## Historical refresh (e.g. simulate the 2018 World Cup with 2018-era data)

Same procedure, swap `World.tsv` for `{year}.tsv`. The schema is
identical. Update the `new_date` in `tools/refresh-elo.awk` to
`31.12.{year}` to reflect that it's a year-end snapshot.

Once a per-competition Elo snapshot lands, historical data would
be loaded per-competition instead of overwriting the bundled CSV; for
now historical refreshes overwrite globally.
