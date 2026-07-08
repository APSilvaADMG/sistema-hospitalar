# Sincroniza bulas extraídas (JSONL) com o banco do APSMedCore.
# Por enquanto usa o índice JSON em Diversos/ (cr-index-bulas.jsonl).

param(
    [switch]$SkipNormalize,
    [switch]$SkipDocker,
    [switch]$FromIndexJson,
    [int]$Batch = 0
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if ($FromIndexJson -or $Batch -eq 0) {
    python scripts/enrich-cr-index-metadata.py
}

if (-not $SkipNormalize) {
    if (Test-Path "data/cr-index-bulas.jsonl") {
        python scripts/normalize-bula-jsonl.py --input data/cr-index-bulas.jsonl
    } else {
        python scripts/normalize-bula-jsonl.py
    }
}

if ($Batch -gt 0) {
    $log = "data/import-logs/batch-$Batch.log"
    python scripts/import-consulta-remedios-bulas.py --batch $Batch --resume --log-file $log
    python scripts/normalize-bula-jsonl.py
}

if (-not $SkipDocker) {
    Write-Host "Reiniciando API para importar JSONL no PostgreSQL..."
    docker compose restart api
    Start-Sleep -Seconds 8
    Write-Host "Teste: GET http://localhost:8080/api/bulario/stats"
}

Write-Host "Concluído. Abra Bulário no frontend e busque por nome (ex.: dipirona)."
