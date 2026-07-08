# Gera massa de dados fictícia para testes de carga (somente banco de TESTE).
param(
    [int]$Patients = 500,
    [int]$Visits = 3,
    [int]$Exams = 4,
    [switch]$Clear,
    [string]$Database = "sistema_hospitalar_test"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent

$env:GTH_ALLOW_LOAD_SEED = "true"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=$Database;Username=postgres;Password=postgres"

$argsList = @(
    "--project", (Join-Path $repoRoot "tools\HospitalLoadSeed\HospitalLoadSeed.csproj"),
    "--",
    "--patients", $Patients,
    "--visits", $Visits,
    "--exams", $Exams
)

if ($Clear) {
    $argsList += "--clear"
}

Write-Host "GTH Load Seed -> Database=$Database Patients=$Patients" -ForegroundColor Yellow
dotnet run @argsList
