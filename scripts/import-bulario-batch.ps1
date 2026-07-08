param(
    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 5)]
    [int]$Batch,

    [double]$Delay = 0.35
)

$ErrorActionPreference = "Continue"
$env:PYTHONIOENCODING = "utf-8"
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$script = Join-Path $root "scripts\import-consulta-remedios-bulas.py"
$output = Join-Path $root "data\consulta-remedios-bulas.jsonl"
$logDir = Join-Path $root "data\import-logs"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$logFile = Join-Path $logDir ("batch-{0}-{1:yyyyMMdd-HHmmss}.log" -f $Batch, (Get-Date))
Write-Host "Lote $Batch/5 -> $logFile"

python $script `
    --batch $Batch `
    --resume `
    --delay $Delay `
    --output $output `
    --log-file $logFile

if ($LASTEXITCODE -ne 0) {
    Write-Error "Importacao falhou com codigo $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Concluido lote $Batch. Log: $logFile"
Write-Host "Reinicie a API para carregar novas bulas no banco (dotnet run)."
