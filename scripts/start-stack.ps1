# Sobe o stack hospitalar sem depender de pull de imagens base (evita falha de DNS no Docker Desktop).
# Uso:
#   .\scripts\start-stack.ps1              # usa imagens api/web já buildadas (recomendado offline)
#   .\scripts\start-stack.ps1 -Build       # rebuild api+web (precisa internet/DNS no Docker)
#   .\scripts\start-stack.ps1 -LocalDev    # só infra no Docker; API e web no host (dotnet + vite)
#   .\scripts\start-stack.ps1 -InfraOnly   # postgres + redis + rabbitmq

param(
    [switch]$Build,
    [switch]$LocalDev,
    [switch]$InfraOnly
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Push-Location $root

function Test-DockerDns {
    Write-Host '==> Testando DNS dentro do Docker...' -ForegroundColor Cyan
    docker run --rm alpine:3.20 nslookup mcr.microsoft.com 2>&1 | Out-Null
    return $LASTEXITCODE -eq 0
}

function Show-DnsHelp {
    Write-Host ''
    Write-Host 'FALHA DE DNS NO DOCKER DESKTOP (lookup mcr.microsoft.com / registry-1.docker.io)' -ForegroundColor Yellow
    Write-Host 'O Windows resolve, mas o motor do Docker nao. Corrija assim:' -ForegroundColor Yellow
    Write-Host '  1. Docker Desktop -> Settings -> Docker Engine'
    Write-Host '     Adicione ou mescle:  "dns": ["8.8.8.8", "1.1.1.1"]'
    Write-Host '  2. Apply & Restart Docker Desktop'
    Write-Host '  3. Se usar VPN, desligue ou configure split tunnel'
    Write-Host '  4. PowerShell admin: wsl --shutdown  (depois abra o Docker de novo)'
    Write-Host ''
    Write-Host 'Enquanto isso, use SEM -Build (imagens ja em cache):' -ForegroundColor Green
    Write-Host '  .\scripts\start-stack.ps1'
    Write-Host ''
    Write-Host 'Ou desenvolvimento local (sem build Docker de api/web):' -ForegroundColor Green
    Write-Host '  .\scripts\start-stack.ps1 -LocalDev'
    Write-Host ''
}

try {
    if ($Build) {
        if (-not (Test-DockerDns)) {
            Show-DnsHelp
            exit 1
        }
        Write-Host '==> docker compose up -d --build --remove-orphans' -ForegroundColor Cyan
        docker compose up -d --build --remove-orphans
        exit $LASTEXITCODE
    }

    if ($InfraOnly) {
        Write-Host '==> Infra: postgres, redis, rabbitmq' -ForegroundColor Cyan
        docker compose up -d postgres redis rabbitmq --remove-orphans
        exit $LASTEXITCODE
    }

    if ($LocalDev) {
        Write-Host '==> Infra no Docker + dev local (API 8080, web 5173)' -ForegroundColor Cyan
        docker compose up -d postgres redis rabbitmq --remove-orphans
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        Write-Host ''
        Write-Host 'Infra OK. Em dois terminais:' -ForegroundColor Green
        Write-Host '  Terminal 1: dotnet run --project src/SistemaHospitalar.Api/SistemaHospitalar.Api.csproj'
        Write-Host '  Terminal 2: cd web; npm run dev'
        Write-Host ''
        Write-Host 'Web: http://localhost:5173   API: http://localhost:8080'
        exit 0
    }

    # Padrao: sobe tudo com imagens locais (sem --build)
    $apiImg = docker images -q sistema-hospitalar-api:latest 2>$null
    $webImg = docker images -q sistema-hospitalar-web:latest 2>$null
    if (-not $apiImg -or -not $webImg) {
        Write-Host 'Imagens api/web nao encontradas no cache local.' -ForegroundColor Yellow
        if (Test-DockerDns) {
            Write-Host 'DNS OK — tentando build...' -ForegroundColor Cyan
            docker compose up -d --build --remove-orphans
            exit $LASTEXITCODE
        }
        Show-DnsHelp
        Write-Host 'Alternativa: .\scripts\start-stack.ps1 -LocalDev' -ForegroundColor Green
        exit 1
    }

    Write-Host '==> docker compose up -d (sem rebuild — imagens em cache)' -ForegroundColor Cyan
    docker compose up -d --remove-orphans
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host ''
    Write-Host 'OK. Web: http://localhost:5173   API: http://localhost:8080/health/ready' -ForegroundColor Green
    Write-Host 'Para rebuild apos corrigir DNS: .\scripts\start-stack.ps1 -Build' -ForegroundColor DarkGray
}
finally {
    Pop-Location
}
