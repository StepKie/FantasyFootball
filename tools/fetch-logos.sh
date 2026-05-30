#!/usr/bin/env bash
# Download confederation + competition logos from Wikipedia / Wikimedia Commons.
# Same pattern as tools/refresh-elo.awk — pinned source URLs, deterministic output.
# Run from repo root:    bash tools/fetch-logos.sh
# Re-run annually to pick up updated logos. SVGs land in
# src/FantasyFootball.UI/wwwroot/img/{confederations,competitions}/.
set -euo pipefail

UA="FantasyFootball-LogoFetch/0.1 (https://github.com/StepKie/FantasyFootball)"
CONF_DIR="src/FantasyFootball.UI/wwwroot/img/confederations"
COMP_DIR="src/FantasyFootball.UI/wwwroot/img/competitions"
mkdir -p "$CONF_DIR" "$COMP_DIR"

# (slug, wiki_host, filename) — wiki_host is "commons" or "en" depending on where the file lives.
# CAF logo is on en.wikipedia (fair-use, not Commons); the rest are on Commons.
fetch() {
  local slug="$1" host="$2" file="$3" outdir="$4"
  local url
  if [ "$host" = "commons" ]; then
    url="https://commons.wikimedia.org/wiki/Special:FilePath/$file"
  else
    url="https://en.wikipedia.org/wiki/Special:FilePath/$file"
  fi
  local out="$outdir/$slug.svg"
  echo "  $slug ← $host:$file"
  # Download to a tmp file and mv on success — `curl -o` truncates the target before transfer, which would wipe a previously-committed SVG on a partial failure.
  local tmp_file
  tmp_file=$(mktemp "${out}.XXXXXX")
  if ! curl -fsSL -A "$UA" -o "$tmp_file" "$url"; then
    rm -f "$tmp_file"
    echo "    ! curl failed for $slug"
    return 1
  fi
  # Sanity: must look like an SVG.
  if ! head -c 9 "$tmp_file" | grep -qE '<\?xml|<svg'; then
    rm -f "$tmp_file"
    echo "    ! $out doesn't look like SVG"
    return 1
  fi
  mv "$tmp_file" "$out"
  sleep 1   # be polite to Wikimedia
}

# Best-effort across all logos: one broken URL shouldn't skip the rest. Accumulate and report at the end.
failed=0
# --- Confederations (7) ---
fetch fifa     commons "FIFA_logo_without_slogan.svg"                         "$CONF_DIR" || failed=1
fetch uefa     en      "UEFA_full_logo.svg"                                   "$CONF_DIR" || failed=1
fetch conmebol en      "CONMEBOL_logo_(2017).svg"                             "$CONF_DIR" || failed=1
fetch concacaf commons "Concacaf_logo.svg"                                    "$CONF_DIR" || failed=1
fetch caf      en      "Confederation_of_African_Football_logo.svg"           "$CONF_DIR" || failed=1
fetch afc      commons "Asian_Football_Confederation_emblem.svg"              "$CONF_DIR" || failed=1
fetch ofc      commons "Oceania_Football_Confederation_logo.svg"              "$CONF_DIR" || failed=1

# --- Competition marks: clean, un-branded canonical versions from football-logos.cc ---
# Wikipedia's infobox files carry sponsor branding (EA Sports, McDonald's, ENILIVE etc.) and
# some are mis-tagged (the "UEFA.svg" Wikipedia file is literally a blank world map). football-
# logos.cc has been hand-curated with clean canonical logos — see tools/fetch-flogos.sh
# for the scraper that walks data-svg-hash attributes on each logo page.
echo "  (run tools/fetch-flogos.sh for the competition + league marks — they come from football-logos.cc)"

if [ "$failed" -eq 0 ]; then
  echo "  done: $(find "$CONF_DIR" -name '*.svg' | wc -l) confederation SVGs"
else
  echo "  done (with failures above): $(find "$CONF_DIR" -name '*.svg' | wc -l) confederation SVGs"
  exit 1
fi
