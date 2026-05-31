#!/usr/bin/env bash
# Download club crests from luukhopman/football-logos (PNG 139×181, top 25 European leagues).
# Per docs/ideas.md, this is the preferred club-crest source. PNG-only — not SVG — but
# rendered small in flag/badge cells the resolution is sufficient and the source is
# reliable + season-versioned (annual repo refresh on the upstream).
#
# Output: src/FantasyFootball.UI/wwwroot/img/clubs/{code-lowercase}.png
# Run from repo root:  bash tools/fetch-club-crests.sh
set -euo pipefail

UA="FantasyFootball-CrestFetch/0.1 (https://github.com/StepKie/FantasyFootball)"
OUT_DIR="src/FantasyFootball.UI/wwwroot/img/clubs"
LEAGUE_DIR="Germany%20-%20Bundesliga"
RAW_BASE="https://raw.githubusercontent.com/luukhopman/football-logos/master/logos/${LEAGUE_DIR}"
mkdir -p "$OUT_DIR"

# Our Team.ShortName → luukhopman repo filename (PNG, with spaces preserved). The repo's
# filenames use full club names + accented chars; we URL-encode at fetch time.
# Filenames are pre-encoded for the URL: spaces → %20, accented chars as their UTF-8
# byte sequence percent-encoded (ö = %C3%B6, ü = %C3%BC). Keeps the fetch script
# dependency-free (no jq / python).
CLUBS=(
  "FCB:Bayern%20Munich.png"
  "BVB:Borussia%20Dortmund.png"
  "RBL:RB%20Leipzig.png"
  "B04:Bayer%2004%20Leverkusen.png"
  "VFB:VfB%20Stuttgart.png"
  "SGE:Eintracht%20Frankfurt.png"
  "SCF:SC%20Freiburg.png"
  "WOB:VfL%20Wolfsburg.png"
  "M05:1.FSV%20Mainz%2005.png"
  "FCU:1.FC%20Union%20Berlin.png"
  "BMG:Borussia%20M%C3%B6nchengladbach.png"
  "FCA:FC%20Augsburg.png"
  "SVW:SV%20Werder%20Bremen.png"
  "TSG:TSG%201899%20Hoffenheim.png"
  "FCH:1.FC%20Heidenheim%201846.png"
  "STP:FC%20St.%20Pauli.png"
  "HSV:Hamburger%20SV.png"
  "KOE:1.FC%20K%C3%B6ln.png"
)

fetch_one() {
  local code="$1" filename="$2"
  local out_file="$OUT_DIR/$(echo "$code" | tr '[:upper:]' '[:lower:]').png"
  local url="${RAW_BASE}/${filename}"
  local tmp_file
  tmp_file=$(mktemp "${out_file}.XXXXXX")
  if ! curl -fsSL -A "$UA" -o "$tmp_file" "$url"; then
    rm -f "$tmp_file"
    echo "  $code  curl failed for $url"
    return 1
  fi
  # Sanity: PNG magic header.
  if ! head -c 8 "$tmp_file" | od -An -tx1 | grep -qi '89 50 4e 47'; then
    rm -f "$tmp_file"
    echo "  $code  $out_file doesn't look like PNG"
    return 1
  fi
  mv "$tmp_file" "$out_file"
  echo "  $code  → $out_file"
}

for entry in "${CLUBS[@]}"; do
  code="${entry%%:*}"
  filename="${entry#*:}"
  fetch_one "$code" "$filename" || true
done
