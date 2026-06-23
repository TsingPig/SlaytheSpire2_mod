<#
.SYNOPSIS
    Restore and build the Ninja mod (Debug by default).
.PARAMETER Configuration
    Build configuration: Debug (default) or Release.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $RepoRoot 'NinjaMod.csproj'

Write-Host "=== Building NinjaMod ($Configuration) ===" -ForegroundColor Cyan

& dotnet restore $proj
if ($LASTEXITCODE -ne 0) { throw "Restore failed (exit $LASTEXITCODE)." }

& dotnet build $proj -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)." }

Write-Host '=== Build succeeded ===' -ForegroundColor Green
