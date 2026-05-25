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
  curl -fsSLo "$out" -A "$UA" "$url"
  # Sanity: must look like an SVG.
  head -c 6 "$out" | grep -qE '<\?xml|<svg' || { echo "    ! $out doesn't look like SVG"; exit 1; }
  sleep 1   # be polite to Wikimedia
}

# --- Confederations (7) ---
fetch fifa     commons "FIFA_logo_without_slogan.svg"                         "$CONF_DIR"
fetch uefa     en      "UEFA_full_logo.svg"                                   "$CONF_DIR"
fetch conmebol en      "CONMEBOL_logo_(2017).svg"                             "$CONF_DIR"
fetch concacaf commons "Concacaf_logo.svg"                                    "$CONF_DIR"
fetch caf      en      "Confederation_of_African_Football_logo.svg"           "$CONF_DIR"
fetch afc      commons "Asian_Football_Confederation_emblem.svg"              "$CONF_DIR"
fetch ofc      commons "Oceania_Football_Confederation_logo.svg"              "$CONF_DIR"

# --- Competition marks: clean, un-branded canonical versions from football-logos.cc ---
# Wikipedia's infobox files carry sponsor branding (EA Sports, McDonald's, ENILIVE etc.) and
# some are mis-tagged (the "UEFA.svg" Wikipedia file is literally a blank world map). football-
# logos.cc has been hand-curated with clean canonical logos — see tools/fetch-flogos.sh
# for the scraper that walks data-svg-hash attributes on each logo page.
echo "  (run tools/fetch-flogos.sh for the competition + league marks — they come from football-logos.cc)"

echo "  done: $(find "$CONF_DIR" "$COMP_DIR" -name '*.svg' | wc -l) SVGs"
