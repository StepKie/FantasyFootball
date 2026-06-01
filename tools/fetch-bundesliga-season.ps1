#Requires -PSEdition Core
# One-off dev tool: fetch the openfootball/deutschland season file and emit
# Definitions/bundesliga-{Season}.json with real matchdays, dates, kickoff times
# and final results. Replaces the algorithmic round-robin generator — the
# openfootball repo is the source of truth (community-maintained, season-versioned).
#
# Run from the repo root: pwsh tools/fetch-bundesliga-season.ps1

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$Seasons = @(
    @{ Season = '2025-2026'; OpenFootballPath = '2025-26/1-bundesliga.txt'; Year = 2026; DefaultYear = 2025; EloSet = 'Bundesliga 2025-2026' }
)

# openfootball uses the official long names; we use short codes on Team.
$NameToCode = @{
    'FC Bayern München'         = 'FCB'
    'Borussia Dortmund'         = 'BVB'
    'RB Leipzig'                = 'RBL'
    'Bayer 04 Leverkusen'       = 'B04'
    'VfB Stuttgart'             = 'VFB'
    'Eintracht Frankfurt'       = 'SGE'
    'SC Freiburg'               = 'SCF'
    'VfL Wolfsburg'             = 'WOB'
    '1. FSV Mainz 05'           = 'M05'
    '1. FC Union Berlin'        = 'FCU'
    'Borussia Mönchengladbach'  = 'BMG'
    'FC Augsburg'               = 'FCA'
    'SV Werder Bremen'          = 'SVW'
    'TSG 1899 Hoffenheim'       = 'TSG'
    '1. FC Heidenheim 1846'     = 'FCH'
    'FC St. Pauli 1910'         = 'STP'
    'Hamburger SV'              = 'HSV'
    '1. FC Köln'                = 'KOE'
}

$Months = @{
    'Jan' = 1;  'Feb' = 2;  'Mar' = 3;  'Apr' = 4;  'May' = 5;  'Jun' = 6
    'Jul' = 7;  'Aug' = 8;  'Sep' = 9;  'Oct' = 10; 'Nov' = 11; 'Dec' = 12
}

# Header has the form "Mon Aug 22 2025" (year optional after the first occurrence).
# Returns a [DateTime] or $null if the line isn't a date header.
function Parse-DateLine {
    param([string]$Line, [int]$DefaultYear)
    if ($Line -notmatch '^\s*(Mon|Tue|Wed|Thu|Fri|Sat|Sun)\s+(\w{3})\s+(\d{1,2})(?:\s+(\d{4}))?\s*$') { return $null }
    $month = $Months[$Matches[2]]
    $day = [int]$Matches[3]
    $year = $Matches[4] ? [int]$Matches[4] : $DefaultYear
    return [DateTime]::new($year, $month, $day)
}

# Game lines: "20:30  Home Team       v Away Team       H-A (HH-AA)" with the time
# optional (only on the first line of a slot). Returns @{ Time, Home, Away, HomeScore, AwayScore } or $null.
function Parse-GameLine {
    param([string]$Line)
    # HT-score parenthetical is optional — openfootball omits it for 0-0 final scores.
    if ($Line -match '^\s*(\d{2}:\d{2})\s+(.+?)\s+v\s+(.+?)\s+(\d+)\s*-\s*(\d+)(?:\s*\(\d+\s*-\s*\d+\))?\s*$') {
        return @{ HasTime = $true; Time = $Matches[1]; Home = $Matches[2].Trim(); Away = $Matches[3].Trim(); HomeScore = [int]$Matches[4]; AwayScore = [int]$Matches[5] }
    }
    if ($Line -match '^\s+(.+?)\s+v\s+(.+?)\s+(\d+)\s*-\s*(\d+)(?:\s*\(\d+\s*-\s*\d+\))?\s*$') {
        return @{ HasTime = $false; Home = $Matches[1].Trim(); Away = $Matches[2].Trim(); HomeScore = [int]$Matches[3]; AwayScore = [int]$Matches[4] }
    }
    return $null
}

function Code { param([string]$Name) if ($NameToCode.ContainsKey($Name)) { return $NameToCode[$Name] } throw "Unmapped openfootball team name: '$Name'" }

$OutputDir = 'src/FantasyFootball.Core/Definitions'

foreach ($s in $Seasons) {
    $url = "https://raw.githubusercontent.com/openfootball/deutschland/master/$($s.OpenFootballPath)"
    Write-Host "Fetching $url..."
    $body = (Invoke-WebRequest -Uri $url -UseBasicParsing).Content

    $games = [System.Collections.Generic.List[object]]::new()
    $seenMatchdays = [System.Collections.Generic.HashSet[int]]::new()
    $rounds = [System.Collections.Generic.List[object]]::new()
    $currentMatchday = 0
    $currentDate = $null
    $currentTime = $null
    $currentYear = $s.DefaultYear
    $gameId = 1

    foreach ($raw in $body -split "`n") {
        $line = $raw.TrimEnd("`r")
        if ($line -match '^▪\s*Matchday\s+(\d+)') {
            $currentMatchday = [int]$Matches[1]
            # openfootball repeats a matchday header for postponed games played later — skip re-emit.
            if ($seenMatchdays.Add($currentMatchday)) {
                $rounds.Add([ordered]@{
                    id = "md-$currentMatchday"
                    name = "Matchday $currentMatchday"
                    stageId = 'season'
                    order = $currentMatchday - 1
                }) | Out-Null
            }
            $currentDate = $null
            $currentTime = $null
            continue
        }
        if ($currentMatchday -eq 0) { continue }

        $maybeDate = Parse-DateLine -Line $line -DefaultYear $currentYear
        if ($maybeDate) { $currentDate = $maybeDate; $currentYear = $maybeDate.Year; continue }

        $game = Parse-GameLine -Line $line
        if ($null -eq $game) { continue }
        if ($null -eq $currentDate) { throw "Game line before any date in matchday $currentMatchday : $line" }
        if ($game.HasTime) { $currentTime = $game.Time }
        if ($null -eq $currentTime) { throw "Game line without preceding time in matchday $currentMatchday : $line" }
        $homeCode = Code $game.Home
        $awayCode = Code $game.Away
        $iso = $currentDate.ToString('yyyy-MM-dd') + 'T' + $currentTime + ':00Z'
        $games.Add([ordered]@{
            kind = 'league'
            homeTeamId = $homeCode
            awayTeamId = $awayCode
            id = $gameId
            playedOn = $iso
            roundId = "md-$currentMatchday"
            result = [ordered]@{ homeScore = $game.HomeScore; awayScore = $game.AwayScore; ending = 'NORMAL' }
        }) | Out-Null
        $gameId++
    }

    $stages = @([ordered]@{ id = 'season'; name = 'Season'; order = 0 })
    $clubs = $NameToCode.Values | Sort-Object
    # Matchdays were collected in encounter order (postponed games make order non-numeric); sort by matchday number for the final JSON.
    $sortedRounds = $rounds | Sort-Object { [int]($_.id -replace '^md-', '') }

    $definition = [ordered]@{
        id = "bundesliga-$($s.Season)"
        title = "Bundesliga $($s.Season)"
        type = 'DOMESTIC_LEAGUE'
        year = $s.Year
        formatId = 'bundesliga-18'
        eloSetName = $s.EloSet
        stages = $stages
        rounds = @($sortedRounds)
        teams = $clubs
        games = $games.ToArray()
    }

    $outPath = Join-Path $OutputDir "bundesliga-$($s.Season).json"
    $definition | ConvertTo-Json -Depth 10 | Set-Content -Path $outPath -Encoding utf8NoBOM
    Write-Host "  → $outPath ($($games.Count) games, $($rounds.Count) matchdays)"
}
