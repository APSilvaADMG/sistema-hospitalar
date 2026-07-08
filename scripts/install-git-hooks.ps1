# Registra hooks do repositório (.githooks/pre-commit).
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Push-Location $root
try {
    git config core.hooksPath .githooks
    Write-Host 'Git hooks instalados: core.hooksPath=.githooks' -ForegroundColor Green
    Write-Host 'O pre-commit roda typecheck (web) e dotnet build quando arquivos .ts/.cs entram no commit.'
}
finally {
    Pop-Location
}
