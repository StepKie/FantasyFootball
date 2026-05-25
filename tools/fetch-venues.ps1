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

# Football stadiums (Q483110 and subclasses) with a known capacity, optional city,
# country (via ISO-3166-1 alpha-3 code), and tenant club. Multi-tenant venues
# return multiple rows we coalesce in post-processing.
$Query = @"
SELECT ?stadium ?stadiumLabel ?cityLabel ?countryCode ?capacity ?clubLabel WHERE {
  ?stadium wdt:P31/wdt:P279* wd:Q483110 .
  ?stadium wdt:P1083 ?capacity .
  FILTER(?capacity >= $MinCapacity)
  FILTER NOT EXISTS { ?stadium wdt:P576 ?dissolved . }   # exclude demolished / dissolved stadiums
  OPTIONAL { ?stadium wdt:P276 ?city. }
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
    $stadiumId = Slugify $stadiumName
    if (-not $stadiumId) { return }

    $cityName = $stadium.cityLabel.value
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

$json = $final | ConvertTo-Json -Depth 4
Set-Content -Path $OutPath -Value $json -Encoding UTF8
Write-Host "Wrote $OutPath"
