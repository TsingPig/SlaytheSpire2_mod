param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"

$processor = Join-Path $ProjectRoot "scripts\process-art.py"
if (-not (Test-Path -LiteralPath $processor)) {
    throw "Missing art processor: $processor"
}

Push-Location $ProjectRoot
try {
    python $processor
    if ($LASTEXITCODE -ne 0) {
        throw "Art processing failed with exit code $LASTEXITCODE."
    }
} finally {
    Pop-Location
}
