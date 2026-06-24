<#
.SYNOPSIS
    Validates that a NinjaMod PCK contains every project PNG and TSCN dependency.

.DESCRIPTION
    Godot export packs PNG files as .import + .ctex pairs and TSCN files as
    .remap + exported .scn pairs. This script parses the PCK directory directly
    and fails if either side of any pair is missing. It catches the broken
    "textures work but the character scene falls back" release case.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$PckPath,

    [string]$WorkspaceRoot
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($WorkspaceRoot)) {
    $WorkspaceRoot = Split-Path -Parent $PSScriptRoot
}
$WorkspaceRoot = (Resolve-Path -LiteralPath $WorkspaceRoot).Path.TrimEnd('\')
$PckPath = (Resolve-Path -LiteralPath $PckPath).Path

$stream = [System.IO.File]::OpenRead($PckPath)
$reader = [System.IO.BinaryReader]::new($stream, [System.Text.Encoding]::UTF8, $true)

try {
    $magic = [System.Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
    if ($magic -ne 'GDPC') {
        throw "Not a Godot PCK: $PckPath"
    }

    $formatVersion = $reader.ReadUInt32()
    $godotMajor = $reader.ReadUInt32()
    $godotMinor = $reader.ReadUInt32()
    $godotPatch = $reader.ReadUInt32()
    $flags = $reader.ReadUInt32()
    $fileBase = $reader.ReadUInt64()
    $directoryOffset = $reader.ReadUInt64()

    $stream.Position = [int64]$directoryOffset
    $entryCount = $reader.ReadUInt32()
    $entries = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)

    for ($i = 0; $i -lt $entryCount; $i++) {
        $pathLength = $reader.ReadUInt32()
        $pathBytes = $reader.ReadBytes([int]$pathLength)
        $path = [System.Text.Encoding]::UTF8.GetString($pathBytes).TrimEnd([char]0)
        $offset = $reader.ReadUInt64()
        $size = $reader.ReadUInt64()
        [void]$reader.ReadBytes(16) # MD5
        [void]$reader.ReadUInt32()  # Entry flags
        $entries[$path] = [pscustomobject]@{ Offset = $offset; Size = $size }
    }

    function Get-EntryText([string]$EntryPath) {
        $entry = $entries[$EntryPath]
        if ($null -eq $entry) {
            throw "PCK entry is missing: $EntryPath"
        }
        if ($entry.Size -gt [int]::MaxValue) {
            throw "PCK text entry is unexpectedly large: $EntryPath"
        }
        $stream.Position = [int64]($fileBase + $entry.Offset)
        return [System.Text.Encoding]::UTF8.GetString($reader.ReadBytes([int]$entry.Size))
    }

    function Assert-RemappedResource([string]$SourcePath, [string]$MetadataPath) {
        if (-not $entries.ContainsKey($MetadataPath)) {
            throw "PCK is missing metadata for $SourcePath ($MetadataPath)"
        }

        $metadata = Get-EntryText $MetadataPath
        $match = [regex]::Match($metadata, 'path="res://([^"]+)"')
        if (-not $match.Success) {
            throw "PCK metadata has no remapped resource path: $MetadataPath"
        }

        $target = $match.Groups[1].Value
        if (-not $entries.ContainsKey($target)) {
            throw "PCK remap target is missing for $SourcePath ($target)"
        }
    }

    $assetRoot = Join-Path $WorkspaceRoot 'NinjaMod'
    $pngFiles = @(Get-ChildItem -LiteralPath $assetRoot -Recurse -File -Filter '*.png')
    $sceneFiles = @(Get-ChildItem -LiteralPath $assetRoot -Recurse -File -Filter '*.tscn')

    foreach ($file in $pngFiles) {
        $relative = $file.FullName.Substring($WorkspaceRoot.Length + 1).Replace('\', '/')
        Assert-RemappedResource $relative "$relative.import"
    }

    foreach ($file in $sceneFiles) {
        $relative = $file.FullName.Substring($WorkspaceRoot.Length + 1).Replace('\', '/')
        Assert-RemappedResource $relative "$relative.remap"
    }

    foreach ($required in @('NinjaMod.json', 'project.binary')) {
        if (-not $entries.ContainsKey($required)) {
            throw "PCK required entry is missing: $required"
        }
    }

    $unexpectedEntries = @($entries.Keys | Where-Object {
        $_ -match '^(temp|dist|scripts)/' -or
        $_ -match '^image-[^/]+\.png\.import$' -or
        $_ -match '^\.godot/imported/image-[^/]+\.ctex$'
    })
    if ($unexpectedEntries.Count -gt 0) {
        throw "PCK contains workspace-only files: $($unexpectedEntries -join ', ')"
    }

    Write-Host "[OK] PCK audit passed: $PckPath" -ForegroundColor Green
    Write-Host "     Godot $godotMajor.$godotMinor.$godotPatch, format $formatVersion, flags $flags"
    Write-Host "     $entryCount packed entries; $($pngFiles.Count) PNG assets; $($sceneFiles.Count) scenes"
}
finally {
    $reader.Dispose()
    $stream.Dispose()
}
