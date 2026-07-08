$ErrorActionPreference = 'Stop'
$base = 'http://localhost:8080/api'
$loginPath = Join-Path $PSScriptRoot '.login-body.json'
if (-not (Test-Path $loginPath)) {
    throw "Arquivo de login ausente: $loginPath"
}

$login = Invoke-RestMethod -Uri "$base/auth/login" -Method POST -ContentType 'application/json' -InFile $loginPath
$headers = @{ Authorization = "Bearer $($login.token)" }

Write-Host '=== ANTES ==='
$before = Invoke-RestMethod -Uri "$base/tiss/billing-catalog-summary" -Headers $headers
$before | ConvertTo-Json

Write-Host ''
Write-Host '=== IMPORTANDO TUSS 202601 (pode demorar varios minutos) ==='
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$import = Invoke-RestMethod -Uri "$base/tiss/tuss-catalog/import-bundled-202601" -Method POST -Headers $headers -TimeoutSec 3600
$sw.Stop()
Write-Host "Tempo: $($sw.Elapsed.TotalMinutes.ToString('F1')) min"
$import | ConvertTo-Json

Write-Host ''
Write-Host '=== CONTAGEM POR TABELA TUSS ==='
foreach ($type in 1..6) {
    $items = Invoke-RestMethod -Uri "$base/tiss/tuss-catalog?tableType=$type" -Headers $headers
    Write-Host "tableType=$type : $($items.Count) itens (amostra endpoint)"
}

Write-Host ''
Write-Host '=== DEPOIS ==='
$after = Invoke-RestMethod -Uri "$base/tiss/billing-catalog-summary" -Headers $headers
$after | ConvertTo-Json
