# Popula banco com 10.000 pacientes fictícios (+ atendimentos, exames, PEP).
# Docker Compose usa sistema_hospitalar por padrão — passe -Database sistema_hospitalar
# (ou rode este script sem -SkipClone após docker stop hospital-api).
param(
    [string]$SourceDatabase = "sistema_hospitalar",
    [string]$Database = "sistema_hospitalar",
    [switch]$ClearFirst,
    [switch]$SkipClone
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent

if (-not $SkipClone) {
    Write-Host "Encerrando conexões e clonando '$SourceDatabase' -> '$Database'..." -ForegroundColor Yellow
    docker exec hospital-postgres psql -U postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname IN ('$SourceDatabase', '$Database') AND pid <> pg_backend_pid();" | Out-Null
    docker exec hospital-postgres psql -U postgres -c "DROP DATABASE IF EXISTS $Database WITH (FORCE);"
    $clone = docker exec hospital-postgres psql -U postgres -c "CREATE DATABASE $Database WITH TEMPLATE $SourceDatabase;" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha ao clonar banco: $clone`nDica: pare a API (docker stop hospital-api) ou use -SkipClone."
    }
    Write-Host "Clone concluído." -ForegroundColor Green
}

$env:GTH_ALLOW_LOAD_SEED = "true"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=$Database;Username=postgres;Password=postgres"

$argsList = @(
    "--project", (Join-Path $repoRoot "tools\HospitalLoadSeed\HospitalLoadSeed.csproj"),
    "--",
    "--skip-base-seed",
    "--patients", "10000",
    "--visits", "5",
    "--exams", "4",
    "--appointments", "1",
    "--pep", "2",
    "--batch", "500"
)

if ($ClearFirst) {
    $argsList += "--clear"
}

Write-Host ""
Write-Host "Iniciando seed de 10.000 pacientes em $Database (vários minutos)..." -ForegroundColor Green
Write-Host "Acompanhe: scripts/load-test/seed-10k.log" -ForegroundColor DarkGray
dotnet run @argsList 2>&1 | Tee-Object -FilePath (Join-Path $repoRoot "scripts\load-test\seed-10k.log")
