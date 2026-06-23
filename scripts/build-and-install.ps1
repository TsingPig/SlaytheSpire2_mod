<#
.SYNOPSIS
    Build the Ninja mod and install it into the game's mods folder.
.PARAMETER Configuration
    Debug (default) or Release.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot

& (Join-Path $here 'build.ps1') -Configuration $Configuration
& (Join-Path $here 'install.ps1') -Configuration $Configuration
