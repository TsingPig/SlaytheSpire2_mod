<#
.SYNOPSIS
    Install the built Ninja mod into the game's mods folder.
.DESCRIPTION
    Copies NinjaMod.dll, NinjaMod.json and (if present) NinjaMod.pck / NinjaMod.pdb into
    <ModsDir>\NinjaMod. Reads GameDir / ModsDir from local.props. Never deletes other mods.
.PARAMETER Configuration
    Which build output to install from: Debug (default) or Release.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
$ProjectName = 'NinjaMod'

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

$GameDir = Get-LocalProp -Name 'GameDir' -Fallback 'D:\SteamLibrary\steamapps\common\Slay the Spire 2'
$ModsDir = Get-LocalProp -Name 'ModsDir' -Fallback (Join-Path $GameDir 'mods')
$destDir = Join-Path $ModsDir $ProjectName

Write-Host "=== Installing $ProjectName -> $destDir ===" -ForegroundColor Cyan

if (-not (Test-Path -LiteralPath $GameDir)) {
    throw "Game directory not found: $GameDir. Edit local.props."
}

$binDir = Join-Path $RepoRoot ".godot\mono\temp\bin\$Configuration"
$dll = Join-Path $binDir "$ProjectName.dll"
if (-not (Test-Path -LiteralPath $dll)) {
    throw "Built DLL not found: $dll. Run scripts\build.ps1 -Configuration $Configuration first."
}

New-Item -ItemType Directory -Path $destDir -Force | Out-Null

# Required files.
Copy-Item -LiteralPath $dll -Destination $destDir -Force
Write-Host "  copied $ProjectName.dll"

$json = Join-Path $RepoRoot "$ProjectName.json"
if (Test-Path -LiteralPath $json) {
    Copy-Item -LiteralPath $json -Destination $destDir -Force
    Write-Host "  copied $ProjectName.json"
} else {
    Write-Warning "  manifest $ProjectName.json not found at repo root."
}

# Optional files (only copied if present).
foreach ($ext in @('pck', 'pdb')) {
    $f = Join-Path $binDir "$ProjectName.$ext"
    if (Test-Path -LiteralPath $f) {
        Copy-Item -LiteralPath $f -Destination $destDir -Force
        Write-Host "  copied $ProjectName.$ext"
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $ModsDir 'BaseLib\BaseLib.json'))) {
    Write-Warning 'BaseLib is not installed in the mods folder. NinjaMod will not load without it. Run scripts\setup.ps1.'
}

Write-Host '=== Install complete ===' -ForegroundColor Green
Write-Host "Installed to: $destDir"
Get-ChildItem -LiteralPath $destDir | Select-Object Name, Length | Format-Table -AutoSize
