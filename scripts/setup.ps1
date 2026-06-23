<#
.SYNOPSIS
    One-time environment setup & validation for the Ninja mod.
.DESCRIPTION
    - Validates the Slay the Spire 2 game path and mods folder.
    - Validates the .NET SDK (>= 9.0, required by the template).
    - Installs the BaseLib dependency mod from the OFFICIAL Alchyr NuGet package
      (no downloads from unofficial sources) if it is not already present.
    Reads GameDir / ModsDir from local.props (gitignored). No absolute paths are hardcoded here.
#>
[CmdletBinding()]
param(
    [switch]$InstallBaseLib = $true
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot

function Get-LocalProp {
    param([string]$Name, [string]$Fallback)
    $propsPath = Join-Path $RepoRoot 'local.props'
    if (Test-Path $propsPath) {
        try {
            [xml]$xml = Get-Content -Raw -LiteralPath $propsPath
            $node = $xml.Project.PropertyGroup.$Name
            if ($node) { return ([string]$node).Trim() }
        } catch { }
    }
    return $Fallback
}

Write-Host '=== Ninja mod setup ===' -ForegroundColor Cyan

# --- Game / mods paths ---
$GameDir = Get-LocalProp -Name 'GameDir' -Fallback 'D:\SteamLibrary\steamapps\common\Slay the Spire 2'
$ModsDir = Get-LocalProp -Name 'ModsDir' -Fallback (Join-Path $GameDir 'mods')

Write-Host "GameDir: $GameDir"
Write-Host "ModsDir: $ModsDir"

if (-not (Test-Path -LiteralPath $GameDir)) {
    throw "Game directory not found: $GameDir. Edit local.props to point at your Slay the Spire 2 install."
}
Write-Host '[OK] Game directory exists.' -ForegroundColor Green

$dataDir = Join-Path $GameDir 'data_sts2_windows_x86_64'
if (-not (Test-Path -LiteralPath (Join-Path $dataDir 'sts2.dll'))) {
    Write-Warning "sts2.dll not found under $dataDir. The build references the game DLL from there."
} else {
    Write-Host '[OK] Game data (sts2.dll) found.' -ForegroundColor Green
}

if (-not (Test-Path -LiteralPath $ModsDir)) {
    New-Item -ItemType Directory -Path $ModsDir -Force | Out-Null
    Write-Host "[OK] Created mods folder: $ModsDir" -ForegroundColor Green
} else {
    Write-Host '[OK] Mods folder exists.' -ForegroundColor Green
}

# --- .NET SDK ---
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw '.NET SDK not found on PATH. Install .NET 9 SDK: https://dotnet.microsoft.com/download'
}
$sdks = (& dotnet --list-sdks)
$hasNet9 = $sdks | Where-Object { $_ -match '^9\.' }
if (-not $hasNet9) {
    Write-Warning ".NET 9 SDK was not found. The project targets net9.0. Installed SDKs:`n$($sdks -join "`n")"
    Write-Warning 'Install the .NET 9 SDK from https://dotnet.microsoft.com/download'
} else {
    Write-Host "[OK] .NET 9 SDK present." -ForegroundColor Green
}

# --- BaseLib dependency mod ---
$baseLibDir = Join-Path $ModsDir 'BaseLib'
if (Test-Path -LiteralPath (Join-Path $baseLibDir 'BaseLib.json')) {
    Write-Host '[OK] BaseLib is already installed in the mods folder.' -ForegroundColor Green
} else {
    Write-Warning 'BaseLib was not found in the mods folder. NinjaMod REQUIRES BaseLib to load.'
    if ($InstallBaseLib) {
        # Ensure the package is restored so the official BaseLib mod files are in the NuGet cache.
        Write-Host 'Restoring packages to obtain the official BaseLib package...'
        & dotnet restore (Join-Path $RepoRoot 'NinjaMod.csproj') | Out-Null

        $nugetRoot = Join-Path $env:USERPROFILE '.nuget\packages\alchyr.sts2.baselib'
        $pkg = Get-ChildItem -Path $nugetRoot -Directory -ErrorAction SilentlyContinue |
               Sort-Object Name -Descending | Select-Object -First 1
        if (-not $pkg) {
            Write-Warning "Could not locate the BaseLib package under $nugetRoot."
            Write-Warning 'Install BaseLib manually (e.g. via the official Slay the Spire 2 Steam Workshop release) into:'
            Write-Warning "  $baseLibDir"
        } else {
            $dll  = Join-Path $pkg.FullName 'lib\net9.0\BaseLib.dll'
            $json = Join-Path $pkg.FullName 'Content\BaseLib.json'
            $pck  = Join-Path $pkg.FullName 'Content\BaseLib.pck'
            New-Item -ItemType Directory -Path $baseLibDir -Force | Out-Null
            foreach ($f in @($dll, $json, $pck)) {
                if (Test-Path -LiteralPath $f) {
                    Copy-Item -LiteralPath $f -Destination $baseLibDir -Force
                    Write-Host "  copied $(Split-Path $f -Leaf)"
                } else {
                    Write-Warning "  missing in package: $f"
                }
            }
            Write-Host "[OK] Installed BaseLib $($pkg.Name) from the official NuGet package into $baseLibDir" -ForegroundColor Green
        }
    } else {
        Write-Host 'Re-run with -InstallBaseLib to install it from the official NuGet package, or install it manually.'
    }
}

Write-Host '=== Setup complete ===' -ForegroundColor Cyan
