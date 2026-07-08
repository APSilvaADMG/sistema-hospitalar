$ErrorActionPreference = 'Stop'
$base = 'http://localhost:8080/api'
$loginPath = Join-Path $PSScriptRoot '.login-body.json'
$login = Invoke-RestMethod -Uri "$base/auth/login" -Method POST -ContentType 'application/json' -InFile $loginPath
$headers = @{ Authorization = "Bearer $($login.token)" }

$summary = Invoke-RestMethod -Uri "$base/tiss/billing-catalog-summary" -Headers $headers
Write-Host '=== RESUMO CATÁLOGO ==='
$summary | ConvertTo-Json

Write-Host ''
Write-Host '=== AMOSTRA POR TIPO (max 200 no endpoint) ==='
$labels = @{ 1 = 'Procedimento'; 2 = 'Material/OPME'; 3 = 'Medicamento'; 4 = 'Diária'; 5 = 'Taxa'; 6 = 'Pacote' }
foreach ($type in 1..6) {
    $items = Invoke-RestMethod -Uri "$base/tiss/tuss-catalog?tableType=$type" -Headers $headers
    Write-Host "$($labels[$type]) (tipo $type): $($items.Count) na amostra"
    if ($items.Count -gt 0) {
        Write-Host "  Ex.: $($items[0].code) - $($items[0].description.Substring(0, [Math]::Min(60, $items[0].description.Length)))"
    }
}
