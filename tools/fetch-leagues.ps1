#Requires -PSEdition Core
# One-off dev tool: generate the data for a domestic-league season — competition
# definition (real fixtures/dates/results), EloSet (season-start ratings), club
# crests, and clubs.json entries — from three community sources:
#
#   * openfootball/{country}        — fixtures + final results (season file)
#   * api.clubelo.com/{date}        — season-start Elo snapshot (Country+Level=1)
#   * luukhopman/football-logos     — club crest PNGs (football-logos.cc front-end)
#
# Each league is a single roster reconciling the three sources' differing club
# names to one short code. Generalizes the former fetch-bundesliga-{season,elo}.ps1
# and fetch-club-crests.sh into one roster-driven generator.
#
# Run from the repo root:  pwsh tools/fetch-leagues.ps1 -Only pl,seriea,laliga,ligue1

param(
    # League keys to (re)generate. Default = the four added on top of Bundesliga.
    [string[]]$Only = @('bundesliga', 'pl', 'seriea', 'laliga', 'ligue1')
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$RepoRoot     = (Resolve-Path "$PSScriptRoot/..").Path
$CompetitionsDir = Join-Path $RepoRoot 'src/FantasyFootball.Core/Resources/Data/Competitions'
$EloSetsDir   = Join-Path $RepoRoot 'src/FantasyFootball.Core/Resources/Data/EloSets'
$ClubsFile    = Join-Path $RepoRoot 'src/FantasyFootball.Core/Resources/Data/clubs.json'
$CrestDir     = Join-Path $RepoRoot 'src/FantasyFootball.UI/wwwroot/img/clubs'
$UA           = 'FantasyFootball-LeagueFetch/0.1 (https://github.com/StepKie/FantasyFootball)'

# ClubElo is one global rating scale, so all leagues share a single combined club EloSet
# (mirrors how national competitions use one all-nations set). Every league definition pins this.
$ClubEloName  = 'Clubs 2025-2026'
$ClubEloDate  = '2025-08-15'   # one ClubElo snapshot near the 2025-26 season start, covering every league

# Roster columns, pipe-delimited:  CODE | openfootball name | clubelo name | crest filename (no .png) | display name (en)
# Codes are unique among clubs; collisions with national-team codes are fine (records aggregate per (isNational, code)).
# Avoid Windows reserved device names as codes (CON, PRN, AUX, NUL, COM1-9, LPT1-9) — the lowercase {code}.png crest is unwritable. (AJ Auxerre is AJA, not AUX.)
$Leagues = @(
    @{
        Key = 'bundesliga'; Type = 'BUNDESLIGA'; Title = 'Bundesliga 2025-2026'; FormatId = 'bundesliga-18'; Country = 'GER'
        OpenFootball = 'deutschland/master/2025-26/1-bundesliga.txt'; CrestRepoDir = 'Germany - Bundesliga'
        QualSlots = @(@(1, 'CHAMPION'), @(2, 3, 4, 'CHAMPIONS_LEAGUE'), @(5, 'EUROPA_LEAGUE'), @(6, 'CONFERENCE_LEAGUE'), @(16, 'RELEGATION_PLAYOFF'), @(17, 18, 'RELEGATED'))
        Roster = @'
FCB|FC Bayern München|Bayern|Bayern Munich|FC Bayern Munich
BVB|Borussia Dortmund|Dortmund|Borussia Dortmund|Borussia Dortmund
RBL|RB Leipzig|RB Leipzig|RB Leipzig|RB Leipzig
B04|Bayer 04 Leverkusen|Leverkusen|Bayer 04 Leverkusen|Bayer 04 Leverkusen
VFB|VfB Stuttgart|Stuttgart|VfB Stuttgart|VfB Stuttgart
SGE|Eintracht Frankfurt|Frankfurt|Eintracht Frankfurt|Eintracht Frankfurt
SCF|SC Freiburg|Freiburg|SC Freiburg|SC Freiburg
WOB|VfL Wolfsburg|Wolfsburg|VfL Wolfsburg|VfL Wolfsburg
M05|1. FSV Mainz 05|Mainz|1.FSV Mainz 05|1. FSV Mainz 05
FCU|1. FC Union Berlin|Union Berlin|1.FC Union Berlin|1. FC Union Berlin
BMG|Borussia Mönchengladbach|Gladbach|Borussia Mönchengladbach|Borussia Mönchengladbach
FCA|FC Augsburg|Augsburg|FC Augsburg|FC Augsburg
SVW|SV Werder Bremen|Werder|SV Werder Bremen|Werder Bremen
TSG|TSG 1899 Hoffenheim|Hoffenheim|TSG 1899 Hoffenheim|TSG 1899 Hoffenheim
FCH|1. FC Heidenheim 1846|Heidenheim|1.FC Heidenheim 1846|1. FC Heidenheim
STP|FC St. Pauli 1910|St Pauli|FC St. Pauli|FC St. Pauli
HSV|Hamburger SV|Hamburg|Hamburger SV|Hamburger SV
KOE|1. FC Köln|Koeln|1.FC Köln|1. FC Köln
'@
    },
    @{
        Key = 'pl'; Type = 'PREMIER_LEAGUE'; Title = 'Premier League 2025-2026'; FormatId = 'pl-20'; Country = 'ENG'
        OpenFootball = 'england/master/2025-26/1-premierleague.txt'; CrestRepoDir = 'England - Premier League'
        QualSlots = @(@(1, 'CHAMPION'), @(2, 3, 4, 'CHAMPIONS_LEAGUE'), @(5, 'EUROPA_LEAGUE'), @(6, 'CONFERENCE_LEAGUE'), @(18, 19, 20, 'RELEGATED'))
        Roster = @'
BOU|AFC Bournemouth|Bournemouth|AFC Bournemouth|AFC Bournemouth
ARS|Arsenal FC|Arsenal|Arsenal FC|Arsenal
AVL|Aston Villa FC|Aston Villa|Aston Villa|Aston Villa
BRE|Brentford FC|Brentford|Brentford FC|Brentford
BHA|Brighton & Hove Albion FC|Brighton|Brighton & Hove Albion|Brighton & Hove Albion
BUR|Burnley FC|Burnley|Burnley FC|Burnley
CHE|Chelsea FC|Chelsea|Chelsea FC|Chelsea
CRY|Crystal Palace FC|Crystal Palace|Crystal Palace|Crystal Palace
EVE|Everton FC|Everton|Everton FC|Everton
FUL|Fulham FC|Fulham|Fulham FC|Fulham
LEE|Leeds United FC|Leeds|Leeds United|Leeds United
LIV|Liverpool FC|Liverpool|Liverpool FC|Liverpool
MCI|Manchester City FC|Man City|Manchester City|Manchester City
MUN|Manchester United FC|Man United|Manchester United|Manchester United
NEW|Newcastle United FC|Newcastle|Newcastle United|Newcastle United
NFO|Nottingham Forest FC|Forest|Nottingham Forest|Nottingham Forest
SUN|Sunderland AFC|Sunderland|Sunderland AFC|Sunderland
TOT|Tottenham Hotspur FC|Tottenham|Tottenham Hotspur|Tottenham Hotspur
WHU|West Ham United FC|West Ham|West Ham United|West Ham United
WOL|Wolverhampton Wanderers FC|Wolves|Wolverhampton Wanderers|Wolverhampton Wanderers
'@
    },
    @{
        Key = 'seriea'; Type = 'SERIE_A'; Title = 'Serie A 2025-2026'; FormatId = 'seriea-20'; Country = 'ITA'
        OpenFootball = 'italy/master/2025-26/1-seriea.txt'; CrestRepoDir = 'Italy - Serie A'
        QualSlots = @(@(1, 'CHAMPION'), @(2, 3, 4, 'CHAMPIONS_LEAGUE'), @(5, 'EUROPA_LEAGUE'), @(6, 'CONFERENCE_LEAGUE'), @(18, 19, 20, 'RELEGATED'))
        Roster = @'
MIL|AC Milan|Milan|AC Milan|AC Milan
PIS|AC Pisa 1909|Pisa|Pisa Sporting Club|Pisa
FIO|ACF Fiorentina|Fiorentina|ACF Fiorentina|Fiorentina
ROM|AS Roma|Roma|AS Roma|AS Roma
ATA|Atalanta BC|Atalanta|Atalanta BC|Atalanta
BOL|Bologna FC 1909|Bologna|Bologna FC 1909|Bologna
CAG|Cagliari Calcio|Cagliari|Cagliari Calcio|Cagliari
COM|Como 1907|Como|Como 1907|Como
INT|FC Internazionale Milano|Inter|Inter Milan|Inter
GEN|Genoa CFC|Genoa|Genoa CFC|Genoa
VER|Hellas Verona FC|Verona|Hellas Verona|Hellas Verona
JUV|Juventus FC|Juventus|Juventus FC|Juventus
PAR|Parma Calcio 1913|Parma|Parma Calcio 1913|Parma
LAZ|SS Lazio|Lazio|SS Lazio|Lazio
NAP|SSC Napoli|Napoli|SSC Napoli|Napoli
TOR|Torino FC|Torino|Torino FC|Torino
CRE|US Cremonese|Cremonese|US Cremonese|Cremonese
LEC|US Lecce|Lecce|US Lecce|Lecce
SAS|US Sassuolo Calcio|Sassuolo|US Sassuolo|Sassuolo
UDI|Udinese Calcio|Udinese|Udinese Calcio|Udinese
'@
    },
    @{
        Key = 'laliga'; Type = 'LA_LIGA'; Title = 'LaLiga 2025-2026'; FormatId = 'laliga-20'; Country = 'ESP'
        OpenFootball = 'espana/master/2025-26/1-liga.txt'; CrestRepoDir = 'Spain - LaLiga'
        QualSlots = @(@(1, 'CHAMPION'), @(2, 3, 4, 'CHAMPIONS_LEAGUE'), @(5, 'EUROPA_LEAGUE'), @(6, 'CONFERENCE_LEAGUE'), @(18, 19, 20, 'RELEGATED'))
        Roster = @'
ATH|Athletic Club|Bilbao|Athletic Bilbao|Athletic Bilbao
OSA|CA Osasuna|Osasuna|CA Osasuna|Osasuna
ATM|Club Atlético de Madrid|Atletico|Atlético de Madrid|Atlético Madrid
ALA|Deportivo Alavés|Alaves|Deportivo Alavés|Deportivo Alavés
ELC|Elche CF|Elche|Elche CF|Elche
BAR|FC Barcelona|Barcelona|FC Barcelona|Barcelona
GET|Getafe CF|Getafe|Getafe CF|Getafe
GIR|Girona FC|Girona|Girona FC|Girona
LEV|Levante UD|Levante|Levante UD|Levante
CEL|RC Celta de Vigo|Celta|Celta de Vigo|Celta Vigo
ESY|RCD Espanyol de Barcelona|Espanyol|RCD Espanyol Barcelona|Espanyol
MLL|RCD Mallorca|Mallorca|RCD Mallorca|Mallorca
RAY|Rayo Vallecano de Madrid|Rayo Vallecano|Rayo Vallecano|Rayo Vallecano
BET|Real Betis Balompié|Betis|Real Betis Balompié|Real Betis
RMA|Real Madrid CF|Real Madrid|Real Madrid|Real Madrid
OVI|Real Oviedo|Oviedo|Real Oviedo|Real Oviedo
RSO|Real Sociedad de Fútbol|Sociedad|Real Sociedad|Real Sociedad
SEV|Sevilla FC|Sevilla|Sevilla FC|Sevilla
VAL|Valencia CF|Valencia|Valencia CF|Valencia
VIL|Villarreal CF|Villarreal|Villarreal CF|Villarreal
'@
    },
    @{
        Key = 'ligue1'; Type = 'LIGUE_1'; Title = 'Ligue 1 2025-2026'; FormatId = 'ligue1-18'; Country = 'FRA'
        OpenFootball = 'france/master/france/2025-26_fr1.txt'; CrestRepoDir = 'France - Ligue 1'
        QualSlots = @(@(1, 'CHAMPION'), @(2, 3, 'CHAMPIONS_LEAGUE'), @(4, 'EUROPA_LEAGUE'), @(5, 'CONFERENCE_LEAGUE'), @(16, 'RELEGATION_PLAYOFF'), @(17, 18, 'RELEGATED'))
        Roster = @'
AJA|AJ Auxerre|Auxerre|AJ Auxerre|Auxerre
ASM|AS Monaco FC|Monaco|AS Monaco|Monaco
ANG|Angers SCO|Angers|Angers SCO|Angers
LOR|FC Lorient|Lorient|FC Lorient|Lorient
MET|FC Metz|Metz|FC Metz|Metz
NAN|FC Nantes|Nantes|FC Nantes|Nantes
LEH|Le Havre AC|Le Havre|Le Havre AC|Le Havre
LIL|Lille OSC|Lille|LOSC Lille|Lille
NIC|OGC Nice|Nice|OGC Nice|Nice
LYO|Olympique Lyonnais|Lyon|Olympique Lyon|Lyon
MAR|Olympique de Marseille|Marseille|Olympique Marseille|Marseille
PFC|Paris FC|Paris FC|Paris FC|Paris FC
PSG|Paris Saint-Germain FC|Paris SG|Paris Saint-Germain|Paris Saint-Germain
STR|RC Strasbourg Alsace|Strasbourg|RC Strasbourg Alsace|Strasbourg
LEN|Racing Club de Lens|Lens|RC Lens|Lens
BRS|Stade Brestois 29|Brest|Stade Brestois 29|Brest
REN|Stade Rennais FC 1901|Rennes|Stade Rennais FC|Rennes
TOU|Toulouse FC|Toulouse|FC Toulouse|Toulouse
'@
    }
)

$Months = @{ 'Jan'=1;'Feb'=2;'Mar'=3;'Apr'=4;'May'=5;'Jun'=6;'Jul'=7;'Aug'=8;'Sep'=9;'Oct'=10;'Nov'=11;'Dec'=12 }

function Parse-DateLine {
    param([string]$Line, [int]$DefaultYear)
    if ($Line -notmatch '^\s*(Mon|Tue|Wed|Thu|Fri|Sat|Sun)\s+(\w{3})\s+(\d{1,2})(?:\s+(\d{4}))?\s*$') { return $null }
    $year = $Matches[4] ? [int]$Matches[4] : $DefaultYear
    return [DateTime]::new($year, $Months[$Matches[2]], [int]$Matches[3])
}

# Game lines: "20:30  Home  v Away  H-A (HH-AA)" — time optional (first line of a slot only),
# HT parenthetical optional. Annotated/cancelled lines (no score) return $null and are skipped.
function Parse-GameLine {
    param([string]$Line)
    if ($Line -match '^\s*(\d{2}:\d{2})\s+(.+?)\s+v\s+(.+?)\s+(\d+)\s*-\s*(\d+)(?:\s*\(\d+\s*-\s*\d+\))?\s*$') {
        return @{ HasTime = $true; Time = $Matches[1]; Home = $Matches[2].Trim(); Away = $Matches[3].Trim(); HomeScore = [int]$Matches[4]; AwayScore = [int]$Matches[5] }
    }
    if ($Line -match '^\s+(.+?)\s+v\s+(.+?)\s+(\d+)\s*-\s*(\d+)(?:\s*\(\d+\s*-\s*\d+\))?\s*$') {
        return @{ HasTime = $false; Home = $Matches[1].Trim(); Away = $Matches[2].Trim(); HomeScore = [int]$Matches[3]; AwayScore = [int]$Matches[4] }
    }
    return $null
}

function ConvertTo-Roster {
    param([string]$Text)
    $map = [ordered]@{}
    foreach ($line in ($Text -split "`n")) {
        $line = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $p = $line -split '\|'
        $map[$p[0]] = @{ Code = $p[0]; OpenFootball = $p[1]; Elo = $p[2]; Crest = $p[3]; En = $p[4] }
    }
    return $map
}

# ConvertTo-Json expands every array multi-line; the committed definitions keep each
# qualificationSlot on one line. Collapse just those objects to match the repo style.
function Compress-QualSlots {
    param([string]$Json)
    return [regex]::Replace($Json, '(?s)\{\s*"positions":\s*\[(.*?)\]\s*,\s*"kind":\s*"(\w+)"\s*\}', {
        param($m)
        $pos = ($m.Groups[1].Value -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }) -join ', '
        '{ "positions": [' + $pos + '], "kind": "' + $m.Groups[2].Value + '" }'
    })
}

function Build-QualSlots {
    param([object[]]$Slots)
    $out = [System.Collections.Generic.List[object]]::new()
    foreach ($s in $Slots) {
        $positions = $s[0..($s.Length - 2)] | ForEach-Object { [int]$_ }
        $out.Add([ordered]@{ positions = @($positions); kind = $s[-1] }) | Out-Null
    }
    return $out.ToArray()
}

$null = New-Item -ItemType Directory -Force -Path $CompetitionsDir, $EloSetsDir, $CrestDir
$newClubs = [ordered]@{}   # code -> @{ En; Country }

foreach ($lg in $Leagues) {
    if ($Only -and ($lg.Key -notin $Only)) { continue }
    Write-Host "`n=== $($lg.Title) [$($lg.Key)] ===" -ForegroundColor Cyan
    $roster = ConvertTo-Roster $lg.Roster
    $ofToCode  = @{}; foreach ($r in $roster.Values) { $ofToCode[$r.OpenFootball] = $r.Code }

    # --- Competition definition (openfootball) ---
    $url = "https://raw.githubusercontent.com/openfootball/$($lg.OpenFootball)"
    Write-Host "  fixtures: $url"
    $body = (Invoke-WebRequest -Uri $url -UseBasicParsing -Headers @{ 'User-Agent' = $UA }).Content
    $startYear = 2025   # calendar year the 2025-26 season opens in; anchors fixture-date parsing

    $games = [System.Collections.Generic.List[object]]::new()
    $rounds = [System.Collections.Generic.List[object]]::new()
    $seen = [System.Collections.Generic.HashSet[int]]::new()
    $md = 0; $date = $null; $time = $null; $year = $startYear; $gid = 1
    foreach ($raw in $body -split "`n") {
        $line = $raw.TrimEnd("`r")
        if ($line -match '^▪\s*Matchday\s+(\d+)') {
            $md = [int]$Matches[1]
            if ($seen.Add($md)) { $rounds.Add([ordered]@{ id = "md-$md"; name = "Matchday $md"; stageId = 'season'; order = $md - 1 }) | Out-Null }
            $date = $null; $time = $null; continue
        }
        if ($md -eq 0) { continue }
        $d = Parse-DateLine -Line $line -DefaultYear $year
        if ($d) { $date = $d; $year = $d.Year; continue }
        $g = Parse-GameLine -Line $line
        if ($null -eq $g) { continue }
        if ($null -eq $date) { throw "Game before any date in matchday $md : $line" }
        if ($g.HasTime) { $time = $g.Time }
        if ($null -eq $time) { throw "Game without preceding time in matchday $md : $line" }
        if (-not $ofToCode.ContainsKey($g.Home)) { throw "Unmapped openfootball home team: '$($g.Home)'" }
        if (-not $ofToCode.ContainsKey($g.Away)) { throw "Unmapped openfootball away team: '$($g.Away)'" }
        $games.Add([ordered]@{
            kind = 'league'; homeTeamId = $ofToCode[$g.Home]; awayTeamId = $ofToCode[$g.Away]
            id = $gid; playedOn = $date.ToString('yyyy-MM-dd') + 'T' + $time + ':00Z'; roundId = "md-$md"
            result = [ordered]@{ homeScore = $g.HomeScore; awayScore = $g.AwayScore; ending = 'NORMAL' }
        }) | Out-Null
        $gid++
    }
    $sortedRounds = $rounds | Sort-Object { [int]($_.id -replace '^md-', '') }
    $definition = [ordered]@{
        id = "$($lg.Key)-2025-2026"; title = $lg.Title; type = $lg.Type; year = 2026
        formatId = $lg.FormatId; eloSetName = $ClubEloName
        qualificationSlots = Build-QualSlots $lg.QualSlots
        stages = @([ordered]@{ id = 'season'; name = 'Season'; order = 0 })
        rounds = @($sortedRounds); teams = @($roster.Keys | Sort-Object); games = $games.ToArray()
    }
    $defPath = Join-Path $CompetitionsDir "$($lg.Key)-2025-2026.json"
    $defJson = Compress-QualSlots ($definition | ConvertTo-Json -Depth 12)
    Set-Content -Path $defPath -Value $defJson -Encoding utf8NoBOM
    Write-Host "  -> $defPath ($($games.Count) games, $($rounds.Count) matchdays)" -ForegroundColor Green

    # --- Crests (luukhopman) ---
    $repoDir = [Uri]::EscapeDataString($lg.CrestRepoDir)
    $okCrests = 0
    foreach ($r in $roster.Values) {
        $file = [Uri]::EscapeDataString("$($r.Crest).png")
        $crestUrl = "https://raw.githubusercontent.com/luukhopman/football-logos/master/logos/$repoDir/$file"
        $out = Join-Path $CrestDir ("{0}.png" -f $r.Code.ToLowerInvariant())
        try {
            Invoke-WebRequest -Uri $crestUrl -UseBasicParsing -Headers @{ 'User-Agent' = $UA } -OutFile $out
            $sig = [System.IO.File]::ReadAllBytes($out)[0..3]
            if (-not ($sig[0] -eq 0x89 -and $sig[1] -eq 0x50 -and $sig[2] -eq 0x4E -and $sig[3] -eq 0x47)) {
                Remove-Item $out; throw 'not a PNG'
            }
            $okCrests++
        }
        catch { Write-Host "    crest FAILED $($r.Code): $crestUrl ($_)" -ForegroundColor Yellow }
    }
    Write-Host "  -> crests: $okCrests/$($roster.Count)" -ForegroundColor Green

    foreach ($r in $roster.Values) { $newClubs[$r.Code] = @{ En = $r.En; Country = $lg.Country } }
}

# --- Combined club EloSet: one ClubElo snapshot covering every league's clubs ---
# Built from all leagues regardless of -Only, since it's a single set every league references.
Write-Host "`n=== $ClubEloName ===" -ForegroundColor Cyan
$eloToCode = @{}
foreach ($lg in $Leagues) { foreach ($r in (ConvertTo-Roster $lg.Roster).Values) { $eloToCode[$r.Elo] = $r.Code } }
$countries = @($Leagues.Country | Select-Object -Unique)
Write-Host "  elo: http://api.clubelo.com/$ClubEloDate"
$rows = (Invoke-WebRequest -Uri "http://api.clubelo.com/$ClubEloDate" -UseBasicParsing -Headers @{ 'User-Agent' = $UA }).Content -split "`n" | Select-Object -Skip 1
$snapshot = [ordered]@{}
foreach ($line in $rows) {
    $line = $line.TrimEnd("`r"); if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $cols = $line -split ','; if ($cols.Length -lt 5) { continue }
    if ($cols[2] -notin $countries -or $cols[3] -ne '1') { continue }
    if ($eloToCode.ContainsKey($cols[1])) { $snapshot[$eloToCode[$cols[1]]] = [int][Math]::Round([double]$cols[4]) }
}
$sortedElo = [ordered]@{}; foreach ($k in ($snapshot.Keys | Sort-Object)) { $sortedElo[$k] = $snapshot[$k] }
if ($sortedElo.Count -ne $eloToCode.Count) {
    $missing = $eloToCode.Values | Where-Object { -not $sortedElo.Contains($_) } | Sort-Object
    Write-Host "  WARNING: mapped $($sortedElo.Count)/$($eloToCode.Count) clubs; no Elo for: $($missing -join ', ')" -ForegroundColor Yellow
}
$eloSet = [ordered]@{ id = 0; name = $ClubEloName; date = $ClubEloDate; teamType = 'CLUB_MEN'; snapshot = $sortedElo }
$eloPath = Join-Path $EloSetsDir 'elo-clubs-2025-2026.json'
$eloSet | ConvertTo-Json -Depth 5 | Set-Content -Path $eloPath -Encoding utf8NoBOM
Write-Host "  -> $eloPath ($($sortedElo.Count) clubs)" -ForegroundColor Green

# --- Merge club entries into clubs.json (append-only; existing entries untouched) ---
$existingCodes = (Get-Content $ClubsFile -Raw | ConvertFrom-Json).code
$toAdd = $newClubs.GetEnumerator() | Where-Object { $_.Key -notin $existingCodes }
if ($toAdd) {
    $entries = $toAdd | ForEach-Object {
        $en = $_.Value.En -replace '\\', '\\' -replace '"', '\"'
@"
  {
    "code": "$($_.Key)",
    "name": {
      "en": "$en"
    },
    "country": "$($_.Value.Country)"
  }
"@
    }
    $txt = (Get-Content $ClubsFile -Raw).TrimEnd()
    $txt = $txt.Substring(0, $txt.LastIndexOf(']')).TrimEnd()
    $txt = $txt + ",`n" + ($entries -join ",`n") + "`n]`n"
    Set-Content -Path $ClubsFile -Value $txt -Encoding utf8NoBOM -NoNewline
    Write-Host "`nAdded $($toAdd.Count) clubs to clubs.json" -ForegroundColor Green
}
else { Write-Host "`nNo new clubs to add to clubs.json" -ForegroundColor Green }
