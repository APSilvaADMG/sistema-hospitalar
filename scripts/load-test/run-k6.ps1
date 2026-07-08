# Teste de carga simultânea na API do GTH (k6).
param(
    [string]$ApiUrl = "http://localhost:8080",
    [string]$Email = "admin@hospital.local",
    [string]$Password = "Admin123!",
    [int]$PeakVus = 25
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$scriptPath = Join-Path $PSScriptRoot "k6-hospital-api.js"

$env:API_URL = $ApiUrl
$env:API_EMAIL = $Email
$env:API_PASSWORD = $Password
$env:K6_VUS_PEAK = "$PeakVus"

Write-Host "GTH k6 load test -> $ApiUrl (peak $PeakVus VUs)" -ForegroundColor Cyan

if (Get-Command k6 -ErrorAction SilentlyContinue) {
    k6 run $scriptPath
    exit $LASTEXITCODE
}

Write-Host "k6 local não encontrado — usando Docker (grafana/k6)..." -ForegroundColor Yellow
docker run --rm -i `
    -e API_URL=$ApiUrl `
    -e API_EMAIL=$Email `
    -e API_PASSWORD=$Password `
    -e K6_VUS_PEAK=$PeakVus `
    -v "${repoRoot}/scripts/load-test:/scripts" `
    grafana/k6 run /scripts/k6-hospital-api.js
