# Gera massa de dados fictícia com simulação hospitalar (somente banco de TESTE).
param(
    [int]$Patients = 500,
    [int]$Visits = 3,
    [int]$Exams = 4,
    [int]$AppointmentsMin = 1,
    [int]$AppointmentsMax = 5,
    [int]$SimulationDays = 30,
    [switch]$Clear,
    [switch]$Simulation,
    [switch]$SimulationOnly,
    [switch]$NoSmartScheduling,
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
    "--exams", $Exams,
    "--appointments-min", $AppointmentsMin,
    "--appointments-max", $AppointmentsMax,
    "--simulation-days", $SimulationDays
)

if ($Clear) {
    $argsList += "--clear"
}

if ($Simulation) {
    $argsList += "--simulation"
}

if ($SimulationOnly) {
    $argsList += "--simulation-only"
}

if ($NoSmartScheduling) {
    $argsList += "--no-smart-scheduling"
}

Write-Host "GTH Hospital Simulation Seed -> Database=$Database Patients=$Patients Simulation=$Simulation" -ForegroundColor Yellow
dotnet run @argsList
