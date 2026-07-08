# Valida API + testes + Web antes do docker compose build (mesmos passos do CI).
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')

Write-Host '==> dotnet build -c Release (API)' -ForegroundColor Cyan
Push-Location $root
try {
    dotnet build -c Release --nologo src/SistemaHospitalar.Api/SistemaHospitalar.Api.csproj
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

Write-Host '==> dotnet test' -ForegroundColor Cyan
Push-Location $root
try {
    dotnet test -c Release --nologo src/SistemaHospitalar.Tests/SistemaHospitalar.Tests.csproj
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

Write-Host '==> npm ci + typecheck + build (web)' -ForegroundColor Cyan
Push-Location (Join-Path $root 'web')
try {
    npm ci
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npx tsc -b
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    npm run build
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}

Write-Host ''
Write-Host 'OK: builds passaram. Pode rodar docker compose up -d --build' -ForegroundColor Green
