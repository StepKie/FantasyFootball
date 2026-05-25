# Bulk-fetch football venue data from Wikidata's public SPARQL endpoint.
# One-time refresh; output bundled at src/FantasyFootball.Core/Resources/Data/venues.json.
#
# Run from repo root:  pwsh ./tools/fetch-venues.ps1
#
# Same pattern as tools/refresh-elo.awk and tools/fetch-logos.sh: pinned source,
# deterministic output. PowerShell is used here (instead of bash) because the
# SPARQL response needs JSON aggregation across multi-tenant venues — bash
# without jq is painful, PowerShell's ConvertFrom-Json handles it cleanly.

$ErrorActionPreference = 'Stop'

$Endpoint = 'https://query.wikidata.org/sparql'
$UA = 'FantasyFootball-VenueFetch/0.1 (https://github.com/StepKie/FantasyFootball)'
$OutPath = 'src/FantasyFootball.Core/Resources/Data/venues.json'
$MinCapacity = 20000

# Football stadiums with known capacity. Multi-tenant venues coalesce in post-process. Walks P131* from P276 to a Q515 ancestor — see commit message for the borough-vs-city handling.
$Query = @"
SELECT ?stadium ?stadiumLabel ?cityLabel ?countryCode ?capacity ?clubLabel WHERE {
  ?stadium wdt:P31/wdt:P279* wd:Q483110 .
  ?stadium wdt:P1083 ?capacity .
  FILTER(?capacity >= $MinCapacity)
  FILTER NOT EXISTS { ?stadium wdt:P576 ?dissolved . }   # exclude demolished / dissolved stadiums
  OPTIONAL {
    ?stadium wdt:P276 ?location .
    ?location wdt:P131* ?city .
    ?city wdt:P31/wdt:P279* wd:Q515 .
  }
  OPTIONAL { ?stadium wdt:P17 ?country. ?country wdt:P298 ?countryCode. }
  OPTIONAL { ?stadium wdt:P466 ?club. }
  SERVICE wikibase:label { bd:serviceParam wikibase:language "en". }
}
ORDER BY DESC(?capacity)
"@

Write-Host "Querying Wikidata SPARQL endpoint (min capacity $MinCapacity)..."
$response = Invoke-RestMethod -Uri $Endpoint `
                              -Headers @{ 'User-Agent' = $UA; 'Accept' = 'application/sparql-results+json' } `
                              -Method Post `
                              -Body @{ query = $Query } `
                              -ContentType 'application/x-www-form-urlencoded'

$rows = $response.results.bindings
Write-Host "  $($rows.Count) result rows (one per stadium-club pair)."

function Slugify($name) {
    if ([string]::IsNullOrWhiteSpace($name)) { return $null }
    $normalized = $name.Normalize([System.Text.NormalizationForm]::FormD)
    $sb = New-Object System.Text.StringBuilder
    foreach ($c in $normalized.ToCharArray()) {
        $cat = [System.Globalization.CharUnicodeInfo]::GetUnicodeCategory($c)
        if ($cat -ne [System.Globalization.UnicodeCategory]::NonSpacingMark) { [void]$sb.Append($c) }
    }
    $ascii = $sb.ToString().Normalize([System.Text.NormalizationForm]::FormC)
    $slug = ($ascii.ToLowerInvariant() -replace '[^a-z0-9]+', '-').Trim('-')
    return $slug
}

# Group by stadium URI, then take the highest-capacity row as the canonical entry
# and aggregate club tenants across rows.
$venues = $rows | Group-Object { $_.stadium.value } | ForEach-Object {
    $stadium = $_.Group[0]
    $stadiumName = $stadium.stadiumLabel.value
    # Wikidata's label service falls back to the raw QID when no English label exists. Slugify would happily turn "Q11836159" into a valid-looking "q11836159" slug, so check the raw label first.
    if ($stadiumName -match '^Q\d+$') { return }
    $stadiumId = Slugify $stadiumName
    if (-not $stadiumId) { return }

    # Picks one of potentially multiple Q515 ancestors for a stadium; in practice the SPARQL chain returns a single city for our 34 competition venues. Ordering across endpoint upgrades isn't speced — if a future run flips between candidates, key the venue in $CityOverrides.
    $cityName = $_.Group |
        Where-Object { $_.cityLabel -and -not [string]::IsNullOrWhiteSpace($_.cityLabel.value) -and $_.cityLabel.value -notmatch '^Q\d+$' } |
        Select-Object -First 1 -ExpandProperty cityLabel |
        Select-Object -ExpandProperty value
    if ([string]::IsNullOrWhiteSpace($cityName)) { $cityName = $null }

    $countryCode = $stadium.countryCode.value
    if ([string]::IsNullOrWhiteSpace($countryCode)) { $countryCode = $null }

    $capacity = [int]$stadium.capacity.value

    $tenantClubs = @($_.Group |
        Where-Object { $_.clubLabel -and $_.clubLabel.value } |
        ForEach-Object { Slugify $_.clubLabel.value } |
        Where-Object { $_ } |
        Sort-Object -Unique)

    [PSCustomObject]@{
        id           = $stadiumId
        name         = $stadiumName
        city         = $cityName
        countryCode  = $countryCode
        capacity     = $capacity
        tenantClubs  = $tenantClubs
    }
} | Where-Object {
    # Drop venues whose only/primary tenant is a Nazi-era site (e.g. the never-built 400k-cap "Deutsches Stadion" with tenant `nazi-party-rally-grounds`). Substring match on slugified tenants — unrelated names with "ss-" prefixes (S.S. Lazio, PSS Sleman, SGS Essen, holy-cross-crusaders) are not affected.
    -not ($_.tenantClubs | Where-Object { $_ -match 'nazi' })
} | Sort-Object capacity -Descending

# Dedupe by slug. Stadiums with identical names occur in two flavors:
#   (a) Different countries → first occurrence keeps the base slug, others get a country-code suffix.
#   (b) Same country (e.g. three Russian "Central Stadium"s with city=null) → fall through to a numeric tiebreaker.
# Order is capacity-desc, so the largest venue keeps the cleanest slug — stable across refreshes
# unless a higher-capacity entrant appears.
$bySlug = @{}
$final = New-Object System.Collections.Generic.List[Object]
foreach ($v in $venues) {
    $base = $v.id
    $cc = if ($v.countryCode) { $v.countryCode.ToLowerInvariant() } else { 'x' }
    $candidates = @($base, "$base-$cc") + (2..99 | ForEach-Object { "$base-$cc-$_" })
    $picked = $candidates | Where-Object { -not $bySlug.ContainsKey($_) } | Select-Object -First 1
    if (-not $picked) { throw "Could not disambiguate $base (too many collisions)" }
    if ($picked -ne $v.id) {
        $v = $v | Select-Object id, name, city, countryCode, capacity, tenantClubs
        $v.id = $picked
    }
    $bySlug[$picked] = $true
    $final.Add($v)
}

Write-Host "  $($final.Count) unique stadiums after dedup."

# Wikidata-gap overrides — keys are post-dedup IDs (relies on the higher-capacity venue keeping the base slug when names collide); see commit message for criteria.
$CityOverrides = @{
    'westfalenstadion'             = 'Dortmund'
    'rheinenergie-stadion'         = 'Cologne'
    'red-bull-arena'               = 'Leipzig'
    'al-bayt-stadium'              = 'Al Khor'
    'khalifa-international-stadium' = 'Al Rayyan'
    'ahmad-bin-ali-stadium'        = 'Al Rayyan'
    'lusail-stadium'               = 'Lusail'
    'education-city-stadium'       = 'Al Rayyan'
    'stadium-974'                  = 'Doha'
    'banorte-stadium'              = 'Mexico City'
    'estadio-akron'                = 'Guadalajara'
    'estadio-monterrey'            = 'Monterrey'
    'metlife-stadium'              = 'East Rutherford'
    'gillette-stadium'             = 'Foxborough'
    'lincoln-financial-field'      = 'Philadelphia'
    'hard-rock-stadium'            = 'Miami Gardens'
    'lumen-field'                  = 'Seattle'
    'ataturk-olympic-stadium'      = 'Istanbul'
    'melbourne-cricket-ground'     = 'Melbourne'
    'estadio-monumental-u'         = 'Lima'
}
$overrideHits = 0
foreach ($v in $final) {
    if ($CityOverrides.ContainsKey($v.id)) {
        $v.city = $CityOverrides[$v.id]
        $overrideHits++
    }
}
Write-Host "  Applied $overrideHits city overrides for Wikidata-gap venues."
if ($overrideHits -ne $CityOverrides.Count) {
    Write-Warning "  Expected $($CityOverrides.Count) override hits but got $overrideHits — a dedup rename may have suffixed an override target's slug."
}

# Berlin Olympiastadion: not returned by the SPARQL query (Wikidata classifies it multi-purpose / Olympic, not Q483110); hand-add.
$berlinId = 'olympiastadion-berlin'
if (-not ($final | Where-Object { $_.id -eq $berlinId })) {
    $berlin = [PSCustomObject]@{
        id           = $berlinId
        name         = 'Olympiastadion'
        city         = 'Berlin'
        countryCode  = 'DEU'
        capacity     = 74475
        tenantClubs  = @('hertha-bsc')
    }
    $final.Add($berlin)
    # Re-sort capacity-desc with the new entry in place.
    $sorted = $final | Sort-Object capacity -Descending
    $final = New-Object System.Collections.Generic.List[Object]
    foreach ($v in $sorted) { $final.Add($v) }
    Write-Host "  Hand-added Berlin Olympiastadion."
}

$json = $final | ConvertTo-Json -Depth 4
Set-Content -Path $OutPath -Value $json -Encoding UTF8
Write-Host "Wrote $OutPath"
