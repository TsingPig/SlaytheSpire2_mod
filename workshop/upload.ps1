# ============================================================
# NinjaMod Steam Workshop Upload Script
# ============================================================
# Prerequisites:
#   1. Steam account that owns Slay the Spire 2
#   2. Steam Guard enabled (required for workshop publishing)
#   3. D:\steamcmd\steamcmd.exe (auto-downloaded if missing)
#
# Usage:
#   Option A (interactive):
#     .\workshop\upload.ps1
#     → Enter Steam username + password when prompted
#     → Enter Steam Guard code when prompted
#
#   Option B (env vars, for CI):
#     $env:STEAM_USER="your_username"
#     $env:STEAM_PASS="your_password"
#     .\workshop\upload.ps1
#
# NOTE: Steam Guard code will ALWAYS be prompted interactively
#       (it changes every login). Keep your phone handy.
# ============================================================

$ErrorActionPreference = "Stop"
$steamcmd = "D:\steamcmd\steamcmd.exe"

if (-not (Test-Path $steamcmd)) {
    Write-Host "[ERROR] steamcmd.exe not found at $steamcmd" -ForegroundColor Red
    Write-Host "Run this first:"
    Write-Host '  New-Item -Type Dir D:\steamcmd -Force'
    Write-Host '  Invoke-WebRequest "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip" -OutFile "$env:TEMP\steamcmd.zip"'
    Write-Host '  Expand-Archive "$env:TEMP\steamcmd.zip" D:\steamcmd -Force'
    exit 1
}

$vdfPath = Join-Path $PSScriptRoot "upload.vdf"
if (-not (Test-Path $vdfPath)) {
    Write-Host "[ERROR] upload.vdf not found at $vdfPath" -ForegroundColor Red
    exit 1
}

Write-Host "======== NinjaMod Workshop Upload ========" -ForegroundColor Cyan
Write-Host "App ID: 2868840 (Slay the Spire 2)" -ForegroundColor Cyan
Write-Host "VDF:    $vdfPath" -ForegroundColor Cyan
Write-Host ""

# Get credentials
$user = $env:STEAM_USER
$pass = $env:STEAM_PASS

if (-not $user) {
    $user = Read-Host "Steam username"
}
if (-not $pass) {
    $secure = Read-Host "Steam password" -AsSecureString
    $pass = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
}

Write-Host ""
Write-Host "Starting SteamCMD..." -ForegroundColor Yellow
Write-Host "If prompted for Steam Guard code, enter it and press Enter." -ForegroundColor Yellow
Write-Host ""

# Build the steamcmd arguments - use +commands
# IMPORTANT: steamcmd on Windows needs interactive terminal for Guard code
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
    Write-Host "  1. Wrong password / Steam Guard code" -ForegroundColor Red
    Write-Host "  2. Steam Guard code expired (codes are only valid ~30s)" -ForegroundColor Red
    Write-Host "  3. You don't own Slay the Spire 2 on this account" -ForegroundColor Red
    Write-Host "  4. Steam servers are down" -ForegroundColor Red
}
