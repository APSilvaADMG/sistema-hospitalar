# Publica branch no GitHub e abre PR — fluxo automatizado (uma vez: login no navegador).
param(
  [string]$Repo = "APSilvaADMG/sistema-hospitalar",
  [string]$Branch = "feature/enterprise-hospital-batch",
  [string]$Base = "main",
  [switch]$SkipPr
)
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root

function Find-Exe([string]$Name, [string[]]$Paths) {
  $cmd = Get-Command $Name -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Source }
  foreach ($p in $Paths) { if (Test-Path $p) { return $p } }
  return $null
}

$git = Find-Exe "git" @(
  "$env:ProgramFiles\Git\cmd\git.exe",
  "${env:ProgramFiles(x86)}\Git\cmd\git.exe",
  "$env:LOCALAPPDATA\Programs\Git\cmd\git.exe"
)
if (-not $git) { throw "Git nao encontrado." }

$gh = Find-Exe "gh" @(
  "$env:ProgramFiles\GitHub CLI\gh.exe",
  "${env:ProgramFiles(x86)}\GitHub CLI\gh.exe"
)

$remoteUrl = "https://github.com/$Repo.git"
$owner = $Repo.Split("/")[0]

Write-Host "==> Repositorio: $Repo" -ForegroundColor Cyan

# Remote
$remotes = & $git remote 2>$null
if ($remotes -notcontains "origin") {
  & $git remote add origin $remoteUrl
} else {
  & $git remote set-url origin $remoteUrl
}

# GitHub CLI
if (-not $gh) {
  Write-Host "Instalando GitHub CLI (winget)..." -ForegroundColor Yellow
  winget install --id GitHub.cli -e --accept-source-agreements --accept-package-agreements | Out-Null
  $gh = Find-Exe "gh" @("$env:ProgramFiles\GitHub CLI\gh.exe")
}
if (-not $gh) { throw "GitHub CLI (gh) nao encontrado apos instalacao." }

# Auth (uma vez)
$authOk = $false
$prevEap = $ErrorActionPreference
$ErrorActionPreference = "SilentlyContinue"
& $gh auth status 2>$null | Out-Null
$authOk = $LASTEXITCODE -eq 0
$ErrorActionPreference = $prevEap

if (-not $authOk) {
  if ($env:GH_TOKEN -or $env:GITHUB_TOKEN) {
    Write-Host "Usando GH_TOKEN / GITHUB_TOKEN do ambiente." -ForegroundColor Green
  } else {
    Write-Host ""
    Write-Host "Login GitHub (uma vez) - o navegador vai abrir." -ForegroundColor Yellow
    Write-Host "Conta esperada: $owner" -ForegroundColor Yellow
    & $gh auth login --hostname github.com --git-protocol https --web
    if ($LASTEXITCODE -ne 0) { throw "gh auth login falhou." }
  }
}

& $gh auth setup-git
if ($LASTEXITCODE -ne 0) { throw "gh auth setup-git falhou." }

# Criar repositorio se ainda nao existir
$ErrorActionPreference = "SilentlyContinue"
& $gh repo view $Repo 2>$null | Out-Null
$repoExists = $LASTEXITCODE -eq 0
$ErrorActionPreference = "Stop"
if (-not $repoExists) {
  Write-Host "==> Criando repositorio https://github.com/$Repo" -ForegroundColor Cyan
  & $gh repo create $Repo --public --description "Sistema hospitalar enterprise (IASGH / Feegow)" --source . --remote origin
  if ($LASTEXITCODE -ne 0) {
    & $gh repo create $Repo --public --description "Sistema hospitalar enterprise (IASGH / Feegow)"
    if ($LASTEXITCODE -ne 0) { throw "gh repo create falhou." }
    & $git remote set-url origin $remoteUrl
  }
}

# Commit pendente
& "$Root\scripts\commit-and-pr.ps1" -Branch $Branch -RemoteUrl $remoteUrl -SkipPush -SkipPr

# Push
Write-Host "==> git push" -ForegroundColor Cyan
& $git push -u origin $Branch
if ($LASTEXITCODE -ne 0) { throw "git push falhou." }

# PR
if (-not $SkipPr) {
  $existing = & $gh pr list --head $Branch --json url -q ".[0].url" 2>$null
  if ($existing) {
    Write-Host "PR existente: $existing" -ForegroundColor Green
  } else {
    Write-Host "==> Criando PR" -ForegroundColor Cyan
    $prBody = @'
## Resumo
- Painel Feegow, sala de espera, TV/voz
- API enums PT-BR (175 tipos)
- Bloqueio de agenda duplicada (DB + servico)
- BI, financeiro, RH/folha, estoque (PDF)
- Relatorios clinicos e gerenciais
- Integracoes WhatsApp/PIX/TISS (status + testes)
- CI: dotnet test (48) + Playwright smoke

Made with [Cursor](https://cursor.com)
'@
    & $gh pr create --repo $Repo --base $Base --head $Branch `
      --title "feat: pacote enterprise hospitalar" `
      --body $prBody
    if ($LASTEXITCODE -ne 0) { throw "gh pr create falhou." }
  }
}

# Badge README
$readme = Join-Path $Root "README.md"
if (Test-Path $readme) {
  $content = Get-Content $readme -Raw
  $badge = "https://github.com/$Repo/actions/workflows/ci.yml/badge.svg?branch=main"
  $link = "https://github.com/$Repo/actions/workflows/ci.yml"
  $content = $content -replace 'https://github.com/OWNER/REPO/actions/workflows/ci\.yml[^)]*', $badge
  $content = $content -replace 'https://github.com/OWNER/REPO/actions/workflows/ci\.yml', $link
  if ($content -notmatch [regex]::Escape($Repo)) {
    $content = $content -replace '\[!\[CI\]\([^)]+\)\]\([^)]+\)', "[![CI]($badge)]($link)"
  }
  Set-Content $readme $content -NoNewline
}

Write-Host ""
Write-Host ("OK. Branch publicada: {0} em {1}" -f $Branch, $Repo) -ForegroundColor Green
& $gh pr view --web 2>$null
