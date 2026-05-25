#!/usr/bin/env bash
# Download SVG logos from football-logos.cc by scraping each logo page for the
# data-svg-hash attribute, then constructing the canonical CDN URL.
set -euo pipefail

UA="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
COMP_DIR="src/FantasyFootball.UI/wwwroot/img/competitions"
mkdir -p "$COMP_DIR"

# fetch_svg <output_slug> <page_path>
# page_path is like "/tournaments/uefa-champions-league/" or "/england/english-premier-league/"
fetch_svg() {
  local out_slug="$1"; local page_path="$2"
  local page_url="https://football-logos.cc$page_path"
  local page_html
  page_html=$(curl -sL -A "$UA" "$page_url")

  local category_id logo_id svg_hash
  category_id=$(printf '%s' "$page_html" | grep -oE 'data-category-id="[^"]+"' | head -1 | sed -E 's/.*"([^"]+)".*/\1/')
  logo_id=$(printf '%s' "$page_html" | grep -oE 'data-logo-id="[^"]+"' | head -1 | sed -E 's/.*"([^"]+)".*/\1/')
  svg_hash=$(printf '%s' "$page_html" | grep -oE 'data-svg-hash="[a-fA-F0-9]+"' | head -1 | sed -E 's/.*"([^"]+)".*/\1/')

  if [ -z "$category_id" ] || [ -z "$logo_id" ] || [ -z "$svg_hash" ]; then
    echo "  $out_slug ← $page_path  (couldn't extract: cat='$category_id' id='$logo_id' hash='$svg_hash')"
    return 1
  fi

  local svg_url="https://images.football-logos.cc/$category_id/$logo_id.$svg_hash.svg"
  local out_file="$COMP_DIR/$out_slug.svg"
  curl -fsSL -A "$UA" -H "Referer: $page_url" -H "Accept: image/svg+xml,*/*" -o "$out_file" "$svg_url"
  if head -c 6 "$out_file" | grep -qE '<\?xml|<svg'; then
    local size=$(wc -c < "$out_file")
    echo "  $out_slug ← $page_path  [$size bytes]"
  else
    echo "  $out_slug ← $page_path  (downloaded but not valid SVG)"
    return 1
  fi
  sleep 1
}

# Best-effort across all logos: one broken page shouldn't skip the rest. Accumulate and report at the end.
failed=0
# Tournaments
fetch_svg wm  "/tournaments/fifa-world-cup-2026/"   || failed=1
fetch_svg em  "/tournaments/uefa-euro-2024/"        || failed=1
fetch_svg ucl "/tournaments/uefa-champions-league/" || failed=1
# Leagues
fetch_svg pl         "/england/english-premier-league/" || failed=1
fetch_svg laliga     "/spain/la-liga/"                  || failed=1
fetch_svg bundesliga "/germany/bundesliga/"             || failed=1
fetch_svg seriea     "/italy/serie-a/"                  || failed=1
fetch_svg ligue1     "/france/ligue-1/"                 || failed=1
fetch_svg mls        "/usa/mls/"                        || failed=1

if [ "$failed" -eq 0 ]; then
  echo "done"
else
  echo "done (with failures above)"
  exit 1
fi
