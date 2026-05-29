# One-off dev tool: scrape eloratings.net year-end TSVs and emit one EloSet JSON
# per tournament-year. Each tournament's pre-tournament Elo is approximated by
# the year-end snapshot of the prior year (i.e. WC 2018 → 2017.tsv), which is
# the finest granularity eloratings.net publishes.
#
# Output: src/FantasyFootball.Core/Resources/Data/EloSets/elo-{year}.json
# matching the EloSet shape: { id, name, date, snapshot: { code3: elo, ... } }.
#
# Run from the repo root: pwsh tools/fetch-elo-sets.ps1
# Not part of the main solution build; data files are what get committed.

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# Tournament-year → (start date, source-year). The source year is the prior
# calendar year because eloratings.net publishes per-year snapshots and a
# tournament's pre-tournament Elo is best approximated by the year-end before.
$Tournaments = @(
    @{ Year = 1962; Date = '1962-05-30'; SourceYear = 1961 }
    @{ Year = 1966; Date = '1966-07-11'; SourceYear = 1965 }
    @{ Year = 1970; Date = '1970-05-31'; SourceYear = 1969 }
    @{ Year = 1980; Date = '1980-06-11'; SourceYear = 1979 }
    @{ Year = 1984; Date = '1984-06-12'; SourceYear = 1983 }
    @{ Year = 1986; Date = '1986-05-31'; SourceYear = 1985 }
    @{ Year = 1988; Date = '1988-06-10'; SourceYear = 1987 }
    @{ Year = 1990; Date = '1990-06-08'; SourceYear = 1989 }
    @{ Year = 1992; Date = '1992-06-10'; SourceYear = 1991 }
    @{ Year = 1994; Date = '1994-06-17'; SourceYear = 1993 }
    @{ Year = 1996; Date = '1996-06-08'; SourceYear = 1995 }
    @{ Year = 1998; Date = '1998-06-10'; SourceYear = 1997 }
    @{ Year = 2000; Date = '2000-06-10'; SourceYear = 1999 }
    @{ Year = 2002; Date = '2002-05-31'; SourceYear = 2001 }
    @{ Year = 2004; Date = '2004-06-12'; SourceYear = 2003 }
    @{ Year = 2006; Date = '2006-06-09'; SourceYear = 2005 }
    @{ Year = 2008; Date = '2008-06-07'; SourceYear = 2007 }
    @{ Year = 2010; Date = '2010-06-11'; SourceYear = 2009 }
    @{ Year = 2012; Date = '2012-06-08'; SourceYear = 2011 }
    @{ Year = 2014; Date = '2014-06-12'; SourceYear = 2013 }
    @{ Year = 2016; Date = '2016-06-10'; SourceYear = 2015 }
    @{ Year = 2018; Date = '2018-06-14'; SourceYear = 2017 }
    @{ Year = 2020; Date = '2021-06-11'; SourceYear = 2020 }  # COVID-delayed; played 2021
    @{ Year = 2022; Date = '2022-11-20'; SourceYear = 2021 }
    @{ Year = 2024; Date = '2024-06-14'; SourceYear = 2023 }
)

# Eloratings.net uses non-ISO 2-letter codes for some teams. countries.json has
# ISO-style codes; the table below patches the mismatches. Anything not listed
# defaults to a direct Code2 → Code3 lookup against countries.json.
$EloratingsCodeOverride = @{
    'EN' = 'ENG'   # England (ISO has GB-ENG)
    'WA' = 'WAL'   # Wales
    'SQ' = 'SCO'   # Scotland (eloratings.net uses SQ, not SC — that's Seychelles)
    'NS' = 'NIR'   # Northern Ireland (NI is Nicaragua)
    'KO' = 'KOS'   # Kosovo
    'NM' = 'MKD'   # North Macedonia
    'SU' = 'URS'   # Soviet Union
    'DD' = 'GDR'   # East Germany
    'WG' = 'FRG'   # West Germany (pre-unification)
    'CS' = 'TCH'   # Czechoslovakia
    'YU' = 'YUG'   # Yugoslavia (1929-2003)
    'SM' = 'SCG'   # Serbia and Montenegro (2003-2006); reuse code? eloratings may use different
    'ZR' = 'ZAI'   # Zaire
    'EI' = 'IRL'   # Eire / Republic of Ireland
    'TI' = 'TAH'   # Tahiti
    'SW' = 'SWZ'   # Eswatini (Swaziland)
}

$CountriesPath = 'src/FantasyFootball.Core/Resources/Data/countries.json'
$OutputDir = 'src/FantasyFootball.Core/Resources/Data/EloSets'
$null = New-Item -ItemType Directory -Force -Path $OutputDir

# Load Code2 → Code3 map from the existing countries.json.
$countries = Get-Content $CountriesPath -Raw | ConvertFrom-Json
$Code2ToCode3 = @{}
foreach ($c in $countries) { $Code2ToCode3[$c.code2] = $c.code3 }

function Resolve-Code3 {
    param([string]$EloratingsCode)
    if ($EloratingsCodeOverride.ContainsKey($EloratingsCode)) {
        return $EloratingsCodeOverride[$EloratingsCode]
    }
    if ($Code2ToCode3.ContainsKey($EloratingsCode)) {
        return $Code2ToCode3[$EloratingsCode]
    }
    return $null
}

$unmappedAll = [System.Collections.Generic.HashSet[string]]::new()

foreach ($t in $Tournaments) {
    $year = $t.Year
    $date = $t.Date
    $src = $t.SourceYear
    $url = "https://www.eloratings.net/$src.tsv"
    Write-Host "Fetching $url for $year ($date)..."

    try {
        $tsv = Invoke-WebRequest -Uri $url -UseBasicParsing
    } catch {
        Write-Warning "Failed to fetch $url : $($_.Exception.Message)"
        continue
    }

    $snapshot = [ordered]@{}
    $unmapped = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $tsv.Content -split "`n") {
        $line = $line.TrimEnd("`r")
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $cols = $line -split "`t"
        if ($cols.Length -lt 4) { continue }
        $eloCode = $cols[2]
        $eloValue = [int]$cols[3]
        $code3 = Resolve-Code3 $eloCode
        if ($null -eq $code3) {
            $unmapped.Add($eloCode) | Out-Null
            $unmappedAll.Add($eloCode) | Out-Null
            continue
        }
        $snapshot[$code3] = $eloValue
    }

    # Sort snapshot by code3 alphabetical for diff stability.
    $sortedSnapshot = [ordered]@{}
    foreach ($key in ($snapshot.Keys | Sort-Object)) {
        $sortedSnapshot[$key] = $snapshot[$key]
    }

    $eloSet = [ordered]@{
        id = 0
        name = "$year"
        date = $date
        snapshot = $sortedSnapshot
    }
    $outPath = Join-Path $OutputDir "elo-$year.json"
    $json = $eloSet | ConvertTo-Json -Depth 5
    Set-Content -Path $outPath -Value $json -Encoding utf8NoBOM
    $count = $sortedSnapshot.Count
    $skipped = $unmapped.Count
    Write-Host "  → $outPath ($count teams, $skipped unmapped)"
}

if ($unmappedAll.Count -gt 0) {
    Write-Host ""
    Write-Host "Unmapped eloratings codes across all fetched years (add to `$EloratingsCodeOverride if relevant):"
    foreach ($code in ($unmappedAll | Sort-Object)) { Write-Host "  $code" }
}
