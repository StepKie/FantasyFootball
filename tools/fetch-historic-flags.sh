#!/usr/bin/env bash
# Download historic-national-team flag SVGs from Wikimedia Commons.
# Same pattern as fetch-logos.sh — pinned source, tmp+mv on success.
# Run from repo root:    bash tools/fetch-historic-flags.sh
# Re-run when adding more historical teams. SVGs land in src/FantasyFootball.UI/wwwroot/img/flags/.
set -euo pipefail

UA="FantasyFootball-FlagFetch/0.1 (https://github.com/StepKie/FantasyFootball)"
OUT_DIR="src/FantasyFootball.UI/wwwroot/img/flags"
mkdir -p "$OUT_DIR"

# fetch <fifa-3-letter-code-lowercase> <commons-filename>
fetch() {
  local slug="$1" file="$2"
  local url="https://commons.wikimedia.org/wiki/Special:FilePath/$file"
  local out="$OUT_DIR/$slug.svg"
  echo "  $slug ← $file"
  local tmp_file
  tmp_file=$(mktemp "${out}.XXXXXX")
  if ! curl -fsSL -A "$UA" -o "$tmp_file" "$url"; then
    rm -f "$tmp_file"
    echo "    ! curl failed for $slug"
    return 1
  fi
  if ! head -c 9 "$tmp_file" | grep -qE '<\?xml|<svg'; then
    rm -f "$tmp_file"
    echo "    ! $out doesn't look like SVG"
    return 1
  fi
  mv "$tmp_file" "$out"
  sleep 1
}

# Defunct national teams that need a local flag (their deprecated ISO alpha-2 codes
# aren't served by flagcdn). FRG shares modern Germany's flag (DE) and TCH shares
# modern Czechia's flag (CZ) — both fall back to flagcdn.
failed=0
fetch gdr "Flag_of_East_Germany.svg"                       || failed=1
fetch urs "Flag_of_the_Soviet_Union.svg"                   || failed=1
# Filenames containing en-dash (–) need URL-encoded to %E2%80%93 — Wikimedia's Special:FilePath does NOT URL-encode the raw UTF-8 byte sequence for us on the wire.
fetch yug "Flag_of_Yugoslavia_(1946%E2%80%931992).svg"             || failed=1
fetch scg "Flag_of_Serbia_and_Montenegro_(1992%E2%80%932006).svg"  || failed=1
fetch zai "Flag_of_Zaire_(1971%E2%80%931997).svg"                  || failed=1

if [ "$failed" -eq 0 ]; then
  echo "  done: $(find "$OUT_DIR" -name '*.svg' | wc -l) historic flag SVGs"
else
  echo "  done (with failures above)"
  exit 1
fi
