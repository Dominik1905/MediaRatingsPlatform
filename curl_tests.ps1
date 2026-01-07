# MRP curl_tests.ps1
# ===========================
# PowerShell test runner for MRP API endpoints.
# Run:
#   powershell -ExecutionPolicy Bypass -File .\curl_tests.ps1
#
# Optional environment overrides:
#   $env:BASE_URL="http://localhost:8080"
#   $env:USER1="robin"; $env:PASS1="Passw0rd!"
#   $env:USER2="can"; $env:PASS2="Passw0rd!"

$ErrorActionPreference = "Stop"

$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:8080" }

$User1 = if ($env:USER1) { $env:USER1 } else { "robin" }
$Pass1 = if ($env:PASS1) { $env:PASS1 } else { "Passw0rd!" }

$User2 = if ($env:USER2) { $env:USER2 } else { "can" }
$Pass2 = if ($env:PASS2) { $env:PASS2 } else { "Passw0rd!" }

function Step($t) { Write-Host ""; Write-Host ("=== {0} ===" -f $t) }

function Invoke-MrpJson {
  param(
    [Parameter(Mandatory=$true)][ValidateSet("GET","POST","PUT","DELETE")] [string]$Method,
    [Parameter(Mandatory=$true)] [string]$Url,
    [string]$Token = "",
    $BodyObj = $null,
    [int[]]$AcceptStatus = @()  # if empty, accept any
  )

  $headers = @{}
  if ($Token -and $Token.Trim().Length -gt 0) {
    $headers["Authorization"] = "Bearer $Token"
  }

  try {
    if ($null -ne $BodyObj) {
      $json = $BodyObj | ConvertTo-Json -Depth 10
      $resp = Invoke-WebRequest -Method $Method -Uri $Url -Headers $headers -ContentType "application/json" -Body $json -UseBasicParsing
    } else {
      $resp = Invoke-WebRequest -Method $Method -Uri $Url -Headers $headers -UseBasicParsing
    }
    if ($AcceptStatus.Count -gt 0 -and ($AcceptStatus -notcontains $resp.StatusCode)) {
      Write-Host "Unexpected HTTP status: $($resp.StatusCode)"
    }
    return @{ Status=$resp.StatusCode; Body=$resp.Content }
  } catch {
    # Web exceptions still contain a response body
    $we = $_.Exception
    if ($we.Response) {
      $r = $we.Response
      $status = [int]$r.StatusCode
      $sr = New-Object System.IO.StreamReader($r.GetResponseStream())
      $body = $sr.ReadToEnd()
      $sr.Close()
      if ($AcceptStatus.Count -gt 0 -and ($AcceptStatus -notcontains $status)) {
        Write-Host "Unexpected HTTP status: $status"
      }
      return @{ Status=$status; Body=$body }
    }
    throw
  }
}

function Try-ParseJson($s) {
  try { return ($s | ConvertFrom-Json -ErrorAction Stop) } catch { return $null }
}

function Get-TokenFromLogin($jsonObj) {
  if ($null -eq $jsonObj) { return "" }
  if ($jsonObj.Token) { return [string]$jsonObj.Token }
  if ($jsonObj.token) { return [string]$jsonObj.token }
  if ($jsonObj.access_token) { return [string]$jsonObj.access_token }
  return ""
}

Step "Config"
Write-Host "BASE_URL=$BaseUrl"
Write-Host "USER1=$User1"
Write-Host "USER2=$User2"

Step "Register users ignore errors if already registered"
$r = Invoke-MrpJson -Method POST -Url "$BaseUrl/api/users/register" -BodyObj @{ Username=$User1; Password=$Pass1 } -AcceptStatus @(200,201,204,400,409)
Write-Host "register $User1 => HTTP $($r.Status)"
$r = Invoke-MrpJson -Method POST -Url "$BaseUrl/api/users/register" -BodyObj @{ Username=$User2; Password=$Pass2 } -AcceptStatus @(200,201,204,400,409)
Write-Host "register $User2 => HTTP $($r.Status)"

Step "Login user1"
$login1 = Invoke-MrpJson -Method POST -Url "$BaseUrl/api/users/login" -BodyObj @{ Username=$User1; Password=$Pass1 } -AcceptStatus @(200,201)
$tok1Obj = Try-ParseJson $login1.Body
$Token1 = Get-TokenFromLogin $tok1Obj
if (-not $Token1) { throw "Could not extract Token1. Login response: $($login1.Body)" }
Write-Host "TOKEN1 ok"

Step "Login user2"
$login2 = Invoke-MrpJson -Method POST -Url "$BaseUrl/api/users/login" -BodyObj @{ Username=$User2; Password=$Pass2 } -AcceptStatus @(200,201)
$tok2Obj = Try-ParseJson $login2.Body
$Token2 = Get-TokenFromLogin $tok2Obj
if (-not $Token2) { throw "Could not extract Token2. Login response: $($login2.Body)" }
Write-Host "TOKEN2 ok"

Step "Create Media as user1"
$createMedia = Invoke-MrpJson -Method POST -Url "$BaseUrl/api/media" -Token $Token1 -BodyObj @{
  Title="Star Wars"; Description="Cooler Film"; Type=0; ReleaseYear=2024; Genre="Action"; AgeRestriction=12
} -AcceptStatus @(200,201)
Write-Host "create media => HTTP $($createMedia.Status)"
$mediaObj = Try-ParseJson $createMedia.Body
$MediaId = ""
if ($mediaObj -and $mediaObj.Id) { $MediaId = [string]$mediaObj.Id }
elseif ($mediaObj -and $mediaObj.id) { $MediaId = [string]$mediaObj.id }
elseif ($mediaObj -and $mediaObj.media -and $mediaObj.media.id) { $MediaId = [string]$mediaObj.media.id }
if (-not $MediaId) { Write-Host "Create response: $($createMedia.Body)"; throw "Could not determine MediaId." }
Write-Host "MEDIA_ID=$MediaId"

Step "List Media filter by titlePart=Star"
$list = Invoke-MrpJson -Method GET -Url "$BaseUrl/api/media?titlePart=Star&sortBy=title&sortOrder=asc" -Token $Token1 -AcceptStatus @(200)
Write-Host "list => HTTP $($list.Status)"
Write-Host $list.Body

Step "Get Media by id"
$get = Invoke-MrpJson -Method GET -Url "$BaseUrl/api/media/$MediaId" -Token $Token1 -AcceptStatus @(200)
Write-Host "get => HTTP $($get.Status)"
Write-Host $get.Body

Step "Update Media as creator user1"
$upd = Invoke-MrpJson -Method PUT -Url "$BaseUrl/api/media/$MediaId" -Token $Token1 -BodyObj @{
  Title="Star Wars (Updated)"; Description="Noch cooler"; Type=0; ReleaseYear=2024; Genre="Action"; AgeRestriction=12
} -AcceptStatus @(200,204)
Write-Host "update owner => HTTP $($upd.Status)"

Step "Update Media as non-owner user2 - expect 403"
$upd2 = Invoke-MrpJson -Method PUT -Url "$BaseUrl/api/media/$MediaId" -Token $Token2 -BodyObj @{
  Title="HACK"; Description=""; Type=0; ReleaseYear=2024; Genre="Action"; AgeRestriction=12
} -AcceptStatus @(403,401,400,404,405)
Write-Host "update non-owner => HTTP $($upd2.Status)"

Step "Create Rating user1"
$rate1 = Invoke-MrpJson -Method POST -Url "$BaseUrl/api/media/$MediaId/ratings" -Token $Token1 -BodyObj @{
  Stars=5; Comment="Mega!"
} -AcceptStatus @(200,201)
Write-Host "rate1 => HTTP $($rate1.Status)"
$rate1Obj = Try-ParseJson $rate1.Body
$Rating1Id = ""
if ($rate1Obj -and $rate1Obj.Id) { $Rating1Id = [string]$rate1Obj.Id }
elseif ($rate1Obj -and $rate1Obj.id) { $Rating1Id = [string]$rate1Obj.id }
if (-not $Rating1Id) { Write-Host $rate1.Body; throw "Could not determine Rating1Id." }
Write-Host "RATING1_ID=$Rating1Id"

Step "Create Rating user2"
$rate2 = Invoke-MrpJson -Method POST -Url "$BaseUrl/api/media/$MediaId/ratings" -Token $Token2 -BodyObj @{
  Stars=4; Comment="Ganz ok"
} -AcceptStatus @(200,201)
Write-Host "rate2 => HTTP $($rate2.Status)"
$rate2Obj = Try-ParseJson $rate2.Body
$Rating2Id = ""
if ($rate2Obj -and $rate2Obj.Id) { $Rating2Id = [string]$rate2Obj.Id }
elseif ($rate2Obj -and $rate2Obj.id) { $Rating2Id = [string]$rate2Obj.id }
if (-not $Rating2Id) { Write-Host $rate2.Body; throw "Could not determine Rating2Id." }
Write-Host "RATING2_ID=$Rating2Id"

Step "List Ratings for Media"
$ratings = Invoke-MrpJson -Method GET -Url "$BaseUrl/api/media/$MediaId/ratings" -Token $Token1 -AcceptStatus @(200)
Write-Host "ratings => HTTP $($ratings.Status)"
Write-Host $ratings.Body

Step "Like user2 rating as user1"
$like = Invoke-MrpJson -Method POST -Url "$BaseUrl/api/ratings/$Rating2Id/like" -Token $Token1 -AcceptStatus @(200,204,409)
Write-Host "like => HTTP $($like.Status)"

Step "Confirm own rating user2 confirms RATING2"
$confirm = Invoke-MrpJson -Method POST -Url "$BaseUrl/api/ratings/$Rating2Id/confirm" -Token $Token2 -AcceptStatus @(200,204,403,404)
Write-Host "confirm => HTTP $($confirm.Status)"

Step "List Ratings for Media after like+confirm"
$ratings2 = Invoke-MrpJson -Method GET -Url "$BaseUrl/api/media/$MediaId/ratings" -Token $Token1 -AcceptStatus @(200)
Write-Host "ratings => HTTP $($ratings2.Status)"
Write-Host $ratings2.Body

Step "Favorite add user1"
$favAdd = Invoke-MrpJson -Method POST -Url "$BaseUrl/api/media/$MediaId/favorite" -Token $Token1 -AcceptStatus @(200,204,409)
Write-Host "favorite add => HTTP $($favAdd.Status)"

Step "Get Favorites user1"
$favs = Invoke-MrpJson -Method GET -Url "$BaseUrl/api/users/$User1/favorites" -Token $Token1 -AcceptStatus @(200,403,401)
Write-Host "favorites => HTTP $($favs.Status)"
Write-Host $favs.Body

Step "Favorite remove user1"
$favRem = Invoke-MrpJson -Method DELETE -Url "$BaseUrl/api/media/$MediaId/favorite" -Token $Token1 -AcceptStatus @(200,204,404)
Write-Host "favorite remove => HTTP $($favRem.Status)"

Step "Get Profile user1"
$prof = Invoke-MrpJson -Method GET -Url "$BaseUrl/api/users/$User1/profile" -Token $Token1 -AcceptStatus @(200,403,401)
Write-Host "profile => HTTP $($prof.Status)"
Write-Host $prof.Body

Step "Get Rating History user1"
$hist = Invoke-MrpJson -Method GET -Url "$BaseUrl/api/users/$User1/ratings" -Token $Token1 -AcceptStatus @(200,403,401)
Write-Host "history => HTTP $($hist.Status)"
Write-Host $hist.Body

Step "Leaderboard"
$lb = Invoke-MrpJson -Method GET -Url "$BaseUrl/api/users/leaderboard" -Token $Token1 -AcceptStatus @(200,401,403)
Write-Host "leaderboard => HTTP $($lb.Status)"
Write-Host $lb.Body

Step "Recommendations user1"
$recs = Invoke-MrpJson -Method GET -Url "$BaseUrl/api/users/$User1/recommendations?limit=10" -Token $Token1 -AcceptStatus @(200,401,403,404)
Write-Host "recs => HTTP $($recs.Status)"
Write-Host $recs.Body

Step "Delete rating user1 deletes own rating1"
$delR = Invoke-MrpJson -Method DELETE -Url "$BaseUrl/api/ratings/$Rating1Id" -Token $Token1 -AcceptStatus @(200,204,403,404)
Write-Host "delete rating => HTTP $($delR.Status)"

Step "Delete Media as creator user1"
$delM = Invoke-MrpJson -Method DELETE -Url "$BaseUrl/api/media/$MediaId" -Token $Token1 -AcceptStatus @(200,204,403,404)
Write-Host "delete media => HTTP $($delM.Status)"

Write-Host ""
Write-Host "DONE."
Write-Host "If any endpoint returned 404, your route names differ - adjust URLs in this script to match your controllers."
