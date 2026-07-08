# Roda typecheck no web após edições do agente (stdin JSON do Cursor é ignorado).
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$web = Join-Path $root 'web'
if (-not (Test-Path (Join-Path $web 'node_modules'))) { exit 0 }
Push-Location $web
try {
    npm run typecheck --silent 2>&1 | Select-Object -Last 15
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
