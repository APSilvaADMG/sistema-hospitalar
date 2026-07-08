# commit-and-pr.ps1
param(
  [string]$Branch = "feature/enterprise-hospital-batch",
  [string]$Remote = "origin",
  [string]$Base = "main",
  [switch]$SkipPush,
  [switch]$SkipPr,
  [string]$RemoteUrl = "https://github.com/APSilvaADMG/sistema-hospitalar.git"
)
$ErrorActionPreference = "Stop"
if (-not $env:GIT_AUTHOR_EMAIL) {
  $env:GIT_AUTHOR_NAME = if ($env:GIT_AUTHOR_NAME) { $env:GIT_AUTHOR_NAME } else { $env:USERNAME }
  $env:GIT_AUTHOR_EMAIL = "noreply@local"
  $env:GIT_COMMITTER_NAME = $env:GIT_AUTHOR_NAME
  $env:GIT_COMMITTER_EMAIL = $env:GIT_AUTHOR_EMAIL
}
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $Root
function Find-Git {
  $cmd = Get-Command git -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Source }
  foreach ($p in @(
    "$env:ProgramFiles\Git\cmd\git.exe",
    "${env:ProgramFiles(x86)}\Git\cmd\git.exe",
    "$env:LOCALAPPDATA\Programs\Git\cmd\git.exe"
  )) { if (Test-Path $p) { return $p } }
  return $null
}
$git = Find-Git
if (-not $git) { throw "Git nao encontrado. Instale Git for Windows." }
function Invoke-Git { param([Parameter(ValueFromRemainingArguments=$true)][string[]]$GitArgs) & $git @GitArgs; if ($LASTEXITCODE -ne 0) { throw "git failed" } }
if (-not (Test-Path ".git")) {
  Invoke-Git init
  if ($RemoteUrl) { Invoke-Git remote add $Remote $RemoteUrl }
}
$current = (& $git branch --show-current 2>$null).Trim()
if (-not $current) { Invoke-Git checkout -b $Branch }
elseif ($current -ne $Branch) { Invoke-Git checkout -b $Branch 2>$null; if ($LASTEXITCODE -ne 0) { Invoke-Git checkout $Branch } }
& $git status -sb
& $git diff --stat
$prevEap = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$null = & $git rev-parse HEAD 2>$null
$hasCommits = $LASTEXITCODE -eq 0
$ErrorActionPreference = $prevEap
if ($hasCommits) { & $git log -5 --oneline }
Invoke-Git add -A
foreach ($pat in @(".env", ".env.local", "scripts/.login-body.json", "tmp-login.json", "seed-run.log")) { & $git reset -q HEAD -- $pat 2>$null }
$staged = (& $git diff --cached --name-only)
if (-not $staged) { Write-Host "Nothing to commit"; exit 0 }
$msg = "feat: pacote enterprise hospitalar (Feegow, PT-BR, integracoes, Playwright, testes)"
Invoke-Git commit -m $msg -m "Dashboard Feegow, sala de espera, enums PT-BR, overlap agenda, BI, financeiro PIX, RH, estoque, impressos gerenciais, status WhatsApp/PIX/TISS, auditoria, testes .NET e smoke E2E."
$hash = (& $git rev-parse HEAD).Trim()
Write-Host "Commit: $hash"
if ($SkipPush) { exit 0 }
if (-not ((& $git remote) -match [regex]::Escape($Remote))) { Write-Warning "Remote ausente"; exit 0 }
Invoke-Git push -u $Remote HEAD
$gh = Get-Command gh -ErrorAction SilentlyContinue
if ($SkipPr -or -not $gh) { exit 0 }
& gh pr create --base $Base --head $Branch --title "feat: pacote enterprise hospitalar" --body "Pacote Feegow, PT-BR, integracoes, Playwright e testes. Ver mensagem do commit.

Made with [Cursor](https://cursor.com)"
