<#
.SYNOPSIS
    Builds, audits, and packages the distributable NinjaMod.zip.
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = (Split-Path -Parent $PSScriptRoot)
$project = Join-Path $root 'NinjaMod.csproj'
$distMod = Join-Path $root 'dist\NinjaMod'
$stagingRoot = Join-Path $root 'temp\release-staging'
$stagingMods = Join-Path $stagingRoot 'mods'
$releaseDir = Join-Path $root 'temp\release'
$zipPath = Join-Path $releaseDir 'NinjaMod.zip'

New-Item -ItemType Directory -Force -Path $stagingMods, $releaseDir | Out-Null

Write-Host "Publishing $Configuration build and full PCK..."
dotnet publish $project -c $Configuration --no-restore "-p:ModsPath=$stagingMods\"
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$requiredFiles = @('NinjaMod.dll', 'NinjaMod.json', 'NinjaMod.pck')
foreach ($name in $requiredFiles) {
    $path = Join-Path $distMod $name
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Release file is missing: $path"
    }
}

& (Join-Path $PSScriptRoot 'audit-pck.ps1') -PckPath (Join-Path $distMod 'NinjaMod.pck') -WorkspaceRoot $root

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
Compress-Archive -LiteralPath $distMod -DestinationPath $zipPath -CompressionLevel Optimal

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $zipEntries = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    foreach ($name in $requiredFiles) {
        $expected = "NinjaMod/$name"
        if ($zipEntries -notcontains $expected) {
            throw "ZIP is missing required entry: $expected"
        }
    }
}
finally {
    $archive.Dispose()
}

$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
Write-Host "[OK] Release package: $zipPath" -ForegroundColor Green
Write-Host "     SHA256: $hash"
