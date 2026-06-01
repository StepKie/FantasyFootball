#Requires -PSEdition Core
# One-off dev tool: fetch ClubElo's date-snapshot for a Bundesliga season-start and emit
# Resources/Data/EloSets/elo-bundesliga-{YYYY-YYYY}.json in the EloSet shape.
#
# ClubElo exposes a global ranking per date at api.clubelo.com/{YYYY-MM-DD}; we filter
# to (Country=GER, Level=1) and map ClubElo's display names to our club codes.
#
# Run from the repo root: pwsh tools/fetch-bundesliga-elo.ps1

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# Bundesliga seasons we want to bake. Date = approximate matchday-1 of the season.
$Seasons = @(
    @{ Season = '2025-2026'; Date = '2025-08-22' }
)

# ClubElo display name → our `Team.ShortName` from clubs.json. Bundesliga 2025-26 set
# (18 clubs); extend when new clubs promote from 2. Bundesliga in future seasons.
$NameToCode = @{
    'Bayern'       = 'FCB'
    'Leverkusen'   = 'B04'
    'Dortmund'     = 'BVB'
    'Frankfurt'    = 'SGE'
    'Stuttgart'    = 'VFB'
    'RB Leipzig'   = 'RBL'
    'Mainz'        = 'M05'
    'Freiburg'     = 'SCF'
    'Werder'       = 'SVW'
    'Wolfsburg'    = 'WOB'
    'Gladbach'     = 'BMG'
    'Augsburg'     = 'FCA'
    'Union Berlin' = 'FCU'
    'Hoffenheim'   = 'TSG'
    'St Pauli'     = 'STP'
    'Heidenheim'   = 'FCH'
    'Hamburg'      = 'HSV'
    'Koeln'        = 'KOE'
}

$OutputDir = 'src/FantasyFootball.Core/Resources/Data/EloSets'
$null = New-Item -ItemType Directory -Force -Path $OutputDir

foreach ($s in $Seasons) {
    $url = "http://api.clubelo.com/$($s.Date)"
    Write-Host "Fetching $url for Bundesliga $($s.Season)..."

    $tsv = Invoke-WebRequest -Uri $url -UseBasicParsing
    $rows = $tsv.Content -split "`n" | Select-Object -Skip 1   # skip header

    $snapshot = [ordered]@{}
    $unmapped = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $rows) {
        $line = $line.TrimEnd("`r")
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $cols = $line -split ','
        if ($cols.Length -lt 5) { continue }
        $name = $cols[1]
        $country = $cols[2]
        $level = $cols[3]
        $elo = [int][Math]::Round([double]$cols[4])
        if ($country -ne 'GER' -or $level -ne '1') { continue }
        if ($NameToCode.ContainsKey($name)) {
            $snapshot[$NameToCode[$name]] = $elo
        } else {
            $unmapped.Add($name) | Out-Null
        }
    }

    # Sort by code for diff stability.
    $sorted = [ordered]@{}
    foreach ($key in ($snapshot.Keys | Sort-Object)) { $sorted[$key] = $snapshot[$key] }

    $eloSet = [ordered]@{
        id = 0
        name = "Bundesliga $($s.Season)"
        date = $s.Date
        snapshot = $sorted
    }
    $outPath = Join-Path $OutputDir "elo-bundesliga-$($s.Season).json"
    $eloSet | ConvertTo-Json -Depth 5 | Set-Content -Path $outPath -Encoding utf8NoBOM
    Write-Host "  → $outPath ($($sorted.Count) clubs, $($unmapped.Count) unmapped)"
    if ($unmapped.Count -gt 0) {
        Write-Host "  Unmapped (add to `$NameToCode if these belong in the league):"
        foreach ($n in $unmapped) { Write-Host "    $n" }
    }
}
