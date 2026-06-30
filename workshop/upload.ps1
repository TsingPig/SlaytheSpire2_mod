# ============================================================
# NinjaMod Steam Workshop Upload Script
# ============================================================
# Prerequisites:
#   1. Steam account that owns Slay the Spire 2
#   2. Steam Guard enabled for workshop publishing
#   3. D:\steamcmd\steamcmd.exe, auto-downloaded if missing
#
# Usage:
#   Option A (interactive):
#     .\workshop\upload.ps1
#     Enter Steam username + password when prompted
#     Enter Steam Guard code when prompted
#
#   Option B (env vars, useful for repeat local uploads):
#     $env:STEAM_USER="your_username"
#     $env:STEAM_PASS="your_password"
#     .\workshop\upload.ps1
#
# NOTE: Steam Guard may still prompt interactively.
#       Keep your Steam Guard device handy.
# ============================================================

param(
    [switch]$ValidateOnly
)

$ErrorActionPreference = "Stop"
$steamcmd = if ($env:STEAMCMD_PATH) { $env:STEAMCMD_PATH } else { "D:\steamcmd\steamcmd.exe" }

if (-not (Test-Path $steamcmd)) {
    $steamcmdDir = Split-Path -Parent $steamcmd
    $zipPath = Join-Path $env:TEMP "steamcmd.zip"
    Write-Host "steamcmd.exe not found at $steamcmd. Downloading SteamCMD..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $steamcmdDir | Out-Null
    Invoke-WebRequest "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip" -OutFile $zipPath
    Expand-Archive $zipPath $steamcmdDir -Force
    Remove-Item -LiteralPath $zipPath -Force
}

if (-not (Test-Path $steamcmd)) {
    throw "steamcmd.exe still not found after install attempt: $steamcmd"
}

$vdfPath = Join-Path $PSScriptRoot "upload.vdf"
if (-not (Test-Path $vdfPath)) {
    Write-Host "[ERROR] upload.vdf not found at $vdfPath" -ForegroundColor Red
    exit 1
}

$vdfText = Get-Content -Raw -LiteralPath $vdfPath
$contentMatch = [regex]::Match($vdfText, '"contentfolder"\s+"([^"]+)"')
$previewMatch = [regex]::Match($vdfText, '"previewfile"\s+"([^"]+)"')
if (-not $contentMatch.Success) { throw "upload.vdf is missing contentfolder." }
if (-not $previewMatch.Success) { throw "upload.vdf is missing previewfile." }

$contentFolder = $contentMatch.Groups[1].Value.Replace("\\", "\")
$previewFile = $previewMatch.Groups[1].Value.Replace("\\", "\")
if (-not (Test-Path -LiteralPath $contentFolder)) { throw "Workshop content folder not found: $contentFolder" }
if (-not (Test-Path -LiteralPath $previewFile)) { throw "Workshop preview file not found: $previewFile" }

foreach ($required in @("NinjaMod.dll", "NinjaMod.json", "NinjaMod.pck")) {
    $requiredPath = Join-Path $contentFolder $required
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Workshop content is missing required file: $requiredPath"
    }
}

Write-Host "======== NinjaMod Workshop Upload ========" -ForegroundColor Cyan
Write-Host "App ID: 2868840 (Slay the Spire 2)" -ForegroundColor Cyan
Write-Host "VDF:    $vdfPath" -ForegroundColor Cyan
Write-Host ""

$publishedIdMatch = [regex]::Match($vdfText, '"publishedfileid"\s+"([^"]+)"')
if ($publishedIdMatch.Success -and $publishedIdMatch.Groups[1].Value -eq "0") {
    Write-Host "publishedfileid is 0: SteamCMD will create a new Workshop item on success." -ForegroundColor Yellow
    Write-Host "After the first upload, update workshop/upload.vdf with the new Workshop ID." -ForegroundColor Yellow
    Write-Host ""
}

if ($ValidateOnly) {
    Write-Host "Validation OK. Workshop content and upload.vdf are ready." -ForegroundColor Green
    exit 0
}

$user = $env:STEAM_USER
$pass = $env:STEAM_PASS

if (-not $user) {
    $user = Read-Host "Steam username"
}
if (-not $pass) {
    $secure = Read-Host "Steam password" -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try {
        $pass = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

Write-Host ""
Write-Host "Starting SteamCMD..." -ForegroundColor Yellow
Write-Host "If prompted for Steam Guard code, enter it and press Enter." -ForegroundColor Yellow
Write-Host ""

$cmdArgs = @(
    "+login", $user, $pass,
    "+workshop_build_item", $vdfPath,
    "+quit"
)

Write-Host "Running: steamcmd +login $user *** +workshop_build_item ...\upload.vdf +quit" -ForegroundColor DarkGray
Write-Host ""

& $steamcmd @cmdArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  UPLOAD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your mod is now on Steam Workshop." -ForegroundColor Green
    Write-Host "Visit: https://steamcommunity.com/app/2868840/workshop/" -ForegroundColor Green
    Write-Host ""
    Write-Host "IMPORTANT: After first upload, note the published file ID" -ForegroundColor Yellow
    Write-Host "and update upload.vdf's 'publishedfileid' from '0' to that ID" -ForegroundColor Yellow
    Write-Host "for future updates." -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "[FAILED] Upload returned exit code $LASTEXITCODE" -ForegroundColor Red
    Write-Host "Common issues:" -ForegroundColor Red
    Write-Host "  1. Wrong password or Steam Guard code" -ForegroundColor Red
    Write-Host "  2. Steam Guard code expired" -ForegroundColor Red
    Write-Host "  3. This Steam account does not own Slay the Spire 2" -ForegroundColor Red
    Write-Host "  4. Steam servers are down" -ForegroundColor Red
}
