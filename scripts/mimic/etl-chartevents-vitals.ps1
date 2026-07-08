#Requires -Version 5.1
<#
.SYNOPSIS
  ETL subset: CHARTEVENTS vital-sign ITEMIDs -> mimic_staging (mimic_iii DB).

.DESCRIPTION
  Streams user-provided CHARTEVENTS.csv(.gz), filters vital ITEMIDs, loads
  mimic_staging.chartevents_vitals_raw, pivots to vital_sign_snapshot.
  Does NOT write to sistema_hospitalar or production VitalSignRecord tables.

.PARAMETER MimicCsvPath
  Folder with MIMIC CSV files (credentialed download).

.PARAMETER MaxSubjects
  Optional cap on distinct SUBJECT_ID rows imported (dev subset).

.EXAMPLE
  .\etl-chartevents-vitals.ps1 -MimicCsvPath "D:\datasets\mimic-iii-clinical-database-1.4" -MaxSubjects 50
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

    [int] $MaxSubjects = 0,
    [switch] $WhatIf
)

$ErrorActionPreference = "Stop"

$VitalItemIds = @(220045, 220179, 220180, 220277, 220210, 223761)
$VitalItemSet = [System.Collections.Generic.HashSet[int]]::new()
foreach ($id in $VitalItemIds) { [void]$VitalItemSet.Add($id) }

$root = Resolve-Path $MimicCsvPath
$chartGz = Join-Path $root "CHARTEVENTS.csv.gz"
$chartCsv = Join-Path $root "CHARTEVENTS.csv"
$chartPath = if (Test-Path $chartGz) { $chartGz } elseif (Test-Path $chartCsv) { $chartCsv } else { $null }
if (-not $chartPath) {
    throw "CHARTEVENTS.csv or CHARTEVENTS.csv.gz not found in $root"
}

$scriptDir = $PSScriptRoot
$schemaSql = Join-Path $scriptDir "001-staging-schema.sql"
$pivotSqlTemplate = Join-Path $scriptDir "002-etl-vital-signs.sql"
if (-not (Test-Path $schemaSql)) { throw "Missing $schemaSql" }
if (-not (Test-Path $pivotSqlTemplate)) { throw "Missing $pivotSqlTemplate" }

$connStr = "Host=$PostgresHost;Port=$PostgresPort;Database=$Database;Username=$Username;Password=$Password"
Write-Host "ETL CHARTEVENTS vitals -> $Database (sandbox)" -ForegroundColor Cyan
Write-Host "Source: $chartPath" -ForegroundColor DarkGray

if ($WhatIf) {
    Write-Host "[WhatIf] Would apply schema, stream CSV, pivot vitals." -ForegroundColor DarkGray
    exit 0
}

# Requires psql in PATH for schema / pivot (standard PostgreSQL client).
function Invoke-PsqlFile([string]$FilePath, [hashtable]$Vars = @{}) {
    $env:PGPASSWORD = $Password
    $args = @("-h", $PostgresHost, "-p", $PostgresPort, "-U", $Username, "-d", $Database, "-v", "ON_ERROR_STOP=1", "-f", $FilePath)
    foreach ($k in $Vars.Keys) {
        $args += "-v"
        $args += "$k=$($Vars[$k])"
    }
    & psql @args
    if ($LASTEXITCODE -ne 0) { throw "psql failed for $FilePath" }
}

function Get-ChartStream([string]$Path) {
    if ($Path.EndsWith(".gz")) {
        $fs = [System.IO.File]::OpenRead($Path)
        return [System.IO.Compression.GZipStream]::new($fs, [System.IO.Compression.CompressionMode]::Decompress)
    }
    return [System.IO.File]::OpenRead($Path)
}

# Apply staging schema
Invoke-PsqlFile $schemaSql

# Start ETL run via psql and capture run id
$env:PGPASSWORD = $Password
$runId = (& psql -h $PostgresHost -p $PostgresPort -U $Username -d $Database -t -A -c `
    "INSERT INTO mimic_staging.etl_run (status, phase, source_path) VALUES ('running', 'chartevents_csv', '$($chartPath -replace '''', '''''')') RETURNING id;").Trim()
if (-not $runId) { throw "Failed to create etl_run row" }
Write-Host "ETL run id: $runId" -ForegroundColor Green

# Truncate raw buffer for this run
& psql -h $PostgresHost -p $PostgresPort -U $Username -d $Database -c "TRUNCATE mimic_staging.chartevents_vitals_raw;" | Out-Null

$subjectCap = [System.Collections.Generic.HashSet[int]]::new()
$rowsInserted = 0
$batch = New-Object System.Collections.Generic.List[string]
$batchSize = 5000

function Flush-Batch {
    if ($batch.Count -eq 0) { return }
    $tempFile = [System.IO.Path]::GetTempFileName()
    try {
        $batch | Set-Content -Path $tempFile -Encoding UTF8
        $env:PGPASSWORD = $Password
        $copySql = "COPY mimic_staging.chartevents_vitals_raw (subject_id, hadm_id, icustay_id, itemid, charttime, valuenum, etl_run_id) FROM STDIN WITH (FORMAT csv, NULL '\N');"
        Get-Content $tempFile | & psql -h $PostgresHost -p $PostgresPort -U $Username -d $Database -c $copySql
        if ($LASTEXITCODE -ne 0) { throw "COPY batch failed" }
    }
    finally {
        Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
        $batch.Clear()
    }
}

try {
    $stream = Get-ChartStream $chartPath
    $reader = New-Object System.IO.StreamReader($stream)
    $header = $reader.ReadLine()
    if (-not $header) { throw "Empty CHARTEVENTS file" }
    $cols = $header.Split(',')
    $idx = @{
        Subject = [array]::IndexOf($cols, "SUBJECT_ID")
        Hadm = [array]::IndexOf($cols, "HADM_ID")
        Icu = [array]::IndexOf($cols, "ICUSTAY_ID")
        Item = [array]::IndexOf($cols, "ITEMID")
        Time = [array]::IndexOf($cols, "CHARTTIME")
        Value = [array]::IndexOf($cols, "VALUENUM")
    }
    foreach ($k in $idx.Keys) {
        if ($idx[$k] -lt 0) { throw "Column $k not found in CHARTEVENTS header" }
    }

    while (-not $reader.EndOfStream) {
        $line = $reader.ReadLine()
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line.Split(',')
        if ($parts.Count -lt $cols.Count) { continue }

        $itemId = [int]$parts[$idx.Item]
        if (-not $VitalItemSet.Contains($itemId)) { continue }

        $subjectId = [int]$parts[$idx.Subject]
        if ($MaxSubjects -gt 0) {
            if (-not $subjectCap.Contains($subjectId)) {
                if ($subjectCap.Count -ge $MaxSubjects) { continue }
                [void]$subjectCap.Add($subjectId)
            }
        }

        $valuenum = $parts[$idx.Value]
        if ([string]::IsNullOrWhiteSpace($valuenum)) { continue }

        $hadm = $parts[$idx.Hadm]
        $icu = $parts[$idx.Icu]
        $hadmOut = if ([string]::IsNullOrWhiteSpace($hadm)) { '\N' } else { $hadm }
        $icuOut = if ([string]::IsNullOrWhiteSpace($icu)) { '\N' } else { $icu }
        $charttime = $parts[$idx.Time]

        $batch.Add("$subjectId,$hadmOut,$icuOut,$itemId,$charttime,$valuenum,$runId")
        $rowsInserted++
        if ($batch.Count -ge $batchSize) {
            Flush-Batch
            if ($rowsInserted % 50000 -eq 0) {
                Write-Host "  ... $rowsInserted raw rows" -ForegroundColor DarkGray
            }
        }
    }
    Flush-Batch
    $reader.Close()
    $stream.Close()

    Write-Host "Loaded $rowsInserted raw vital rows." -ForegroundColor Green

    $pivotSql = Get-Content $pivotSqlTemplate -Raw
    $pivotSql = $pivotSql -replace ':etl_run_id', $runId
    $pivotFile = [System.IO.Path]::GetTempFileName() + ".sql"
    Set-Content -Path $pivotFile -Value $pivotSql -Encoding UTF8
    try {
        Invoke-PsqlFile $pivotFile
    }
    finally {
        Remove-Item $pivotFile -Force -ErrorAction SilentlyContinue
    }

    $snapshotCount = (& psql -h $PostgresHost -p $PostgresPort -U $Username -d $Database -t -A -c `
        "SELECT COUNT(*) FROM mimic_staging.vital_sign_snapshot WHERE etl_run_id = $runId;").Trim()

    & psql -h $PostgresHost -p $PostgresPort -U $Username -d $Database -c `
        "UPDATE mimic_staging.etl_run SET status = 'completed', completed_at = NOW(), rows_processed = $rowsInserted, phase = 'completed' WHERE id = $runId;" | Out-Null

    Write-Host "Pivot complete: $snapshotCount vital_sign_snapshot rows (run $runId)." -ForegroundColor Green
}
catch {
    $msg = $_.Exception.Message -replace '''', ''''''
    & psql -h $PostgresHost -p $PostgresPort -U $Username -d $Database -c `
        "UPDATE mimic_staging.etl_run SET status = 'failed', completed_at = NOW(), error_message = '$msg' WHERE id = $runId;" | Out-Null
    throw
}
