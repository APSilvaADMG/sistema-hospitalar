#Requires -Version 5.1
<#
.SYNOPSIS
  Validação e importação subset MIMIC-III para PostgreSQL sandbox (não baixa dados).

.DESCRIPTION
  Valida um diretório de CSV credenciado pelo usuário e orienta (ou executa) a carga em banco
  SEPARADO do sistema_hospitalar de produção. Não mistura PHI real com MIMIC.

  Pré-requisitos PhysioNet: conta, CITI, DUA assinado, download manual v1.4.
  https://physionet.org/content/mimiciii/1.4/

.PARAMETER MimicCsvPath
  Pasta com os CSV extraídos (ex.: mimic-iii-clinical-database-1.4).

.PARAMETER SubsetOnly
  Valida apenas tabelas do subset de desenvolvimento (menor volume).

.PARAMETER RunEtl
  Após validação, executa ETL CHARTEVENTS -> mimic_staging (etl-chartevents-vitals.ps1).

.PARAMETER MaxSubjects
  Limite de SUBJECT_ID no ETL de sinais vitais (0 = sem limite).

.EXAMPLE
  .\import-mimic-subset.ps1 -MimicCsvPath "D:\datasets\mimic-iii-clinical-database-1.4" -SubsetOnly -RunEtl -MaxSubjects 50
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $MimicCsvPath,

    [string] $PostgresHost = "localhost",
    [int] $PostgresPort = 5432,
    [string] $Database = "mimic_iii",
    [string] $Username = "postgres",
    [string] $Password = "postgres",

    [switch] $SubsetOnly,
    [switch] $RunEtl,
    [int] $MaxSubjects = 50,
    [switch] $WhatIf
)

$ErrorActionPreference = "Stop"

$Warning = @"
================================================================================
 AVISO: MIMIC-III é apenas para pesquisa/demo em banco ISOLADO.
 - NÃO importe no database sistema_hospitalar de produção.
 - NÃO misture com pacientes reais (LGPD/HIPAA).
 - Respeite o DUA PhysioNet e proíba reidentificação.
================================================================================
"@

Write-Host $Warning -ForegroundColor Yellow

$root = Resolve-Path $MimicCsvPath
$mappingPath = Join-Path $PSScriptRoot "table-mapping.json"
if (-not (Test-Path $mappingPath)) {
    throw "Arquivo de mapeamento não encontrado: $mappingPath"
}

$mapping = Get-Content $mappingPath -Raw | ConvertFrom-Json
$allTables = @(
    "ADMISSIONS", "CHARTEVENTS", "CPTEVENTS", "D_CPT", "D_ICD_DIAGNOSES",
    "D_ICD_PROCEDURES", "D_ITEMS", "D_LABITEMS", "DATETIMEEVENTS", "DIAGNOSES_ICD",
    "DRGCODES", "ICUSTAYS", "INPUTEVENTS_CV", "INPUTEVENTS_MV", "LABEVENTS",
    "MICROBIOLOGYEVENTS", "NOTEEVENTS", "OUTPUTEVENTS", "PATIENTS", "PRESCRIPTIONS",
    "PROCEDUREEVENTS_MV", "PROCEDURES_ICD", "SERVICES", "TRANSFERS"
)

$tables = if ($SubsetOnly) { @($mapping.subsetTablesForDev) } else { $allTables }

Write-Host "`nValidando CSV em: $root" -ForegroundColor Cyan
$missing = @()
$found = @()
foreach ($t in $tables) {
    $gz = Join-Path $root "$t.csv.gz"
    $csv = Join-Path $root "$t.csv"
    if (Test-Path $gz) { $found += $gz }
    elseif (Test-Path $csv) { $found += $csv }
    else { $missing += $t }
}

if ($missing.Count -gt 0) {
    Write-Host "Arquivos ausentes ($($missing.Count)):" -ForegroundColor Red
    $missing | ForEach-Object { Write-Host "  - $_" }
    Write-Host "`nObtenha os arquivos após credenciamento em https://physionet.org/content/mimiciii/1.4/" -ForegroundColor Yellow
    exit 1
}

Write-Host "OK: $($found.Count) arquivo(s) encontrado(s)." -ForegroundColor Green

$createDbSql = @"
-- Execute como superuser em PostgreSQL SEPARADO (não sistema_hospitalar)
CREATE DATABASE $Database;
"@

Write-Host "`n--- Próximos passos ---" -ForegroundColor Cyan
Write-Host "1. Criar banco sandbox (se ainda não existir):"
Write-Host $createDbSql
Write-Host "2. Carga nativa MIMIC (opcional): scripts oficiais em https://github.com/MIT-LCP/mimic-code"
Write-Host "3. ETL subset sinais vitais: .\etl-chartevents-vitals.ps1 -MimicCsvPath `"$root`" -MaxSubjects $MaxSubjects"
Write-Host "4. Configurar MimicResearch:ConnectionString -> database=$Database"
Write-Host "5. Configurar MimicResearch:CsvPath -> pasta do download"
Write-Host "6. Manter MimicResearch:Enabled=false em produção."

if ($WhatIf) {
    Write-Host "`n[WhatIf] Nenhuma carga executada." -ForegroundColor DarkGray
    exit 0
}

if ($RunEtl) {
    $etlScript = Join-Path $PSScriptRoot "etl-chartevents-vitals.ps1"
    if (-not (Test-Path $etlScript)) {
        throw "Script ETL não encontrado: $etlScript"
    }

    Write-Host "`nExecutando ETL subset (CHARTEVENTS vitals)..." -ForegroundColor Cyan
    & $etlScript -MimicCsvPath $root `
        -PostgresHost $PostgresHost `
        -PostgresPort $PostgresPort `
        -Database $Database `
        -Username $Username `
        -Password $Password `
        -MaxSubjects $MaxSubjects
    exit $LASTEXITCODE
}

Write-Host "`nValidação concluída. Use -RunEtl para carregar mimic_staging ou POST /api/research/mimic/etl/import (dev)." -ForegroundColor Green
