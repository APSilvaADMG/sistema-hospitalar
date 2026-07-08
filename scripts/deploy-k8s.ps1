<#
.SYNOPSIS
  Aplica os manifests Kubernetes do Sistema Hospitalar em ordem.

.DESCRIPTION
  Namespace padrão: sistema-hospitalar.
  Segredos NÃO são commitados — crie o Secret hospital-secrets antes (ou use -CreateExampleSecret).

.PARAMETER Namespace
  Namespace Kubernetes (default: sistema-hospitalar).

.PARAMETER ImageRegistry
  Prefixo de registry para substituir imagens (ex.: ghcr.io/apsilvaadmg).
  Vazio = mantém o valor dos manifests.

.PARAMETER ApiImage
  Override completo da imagem da API.

.PARAMETER WebImage
  Override completo da imagem do web.

.PARAMETER CreateExampleSecret
  Se o secret não existir, cria a partir de k8s/secret.example.yaml (REPLACE_ME — só para lab).

.PARAMETER SkipWait
  Não aguarda rollout dos deployments.

.EXAMPLE
  .\scripts\deploy-k8s.ps1

.EXAMPLE
  .\scripts\deploy-k8s.ps1 -ApiImage sistema-hospitalar-api:local -WebImage sistema-hospitalar-web:local

.EXAMPLE
  .\scripts\deploy-k8s.ps1 -ImageRegistry "ghcr.io/apsilvaadmg"
#>
[CmdletBinding()]
param(
  [string]$Namespace = "sistema-hospitalar",
  [string]$ImageRegistry = "",
  [string]$ApiImage = "",
  [string]$WebImage = "",
  [switch]$CreateExampleSecret,
  [switch]$SkipWait
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$k8s = Join-Path $root "k8s"

function Test-Command($name) {
  return [bool](Get-Command $name -ErrorAction SilentlyContinue)
}

if (-not (Test-Command kubectl)) {
  throw "kubectl não encontrado no PATH."
}

Write-Host "==> Namespace: $Namespace" -ForegroundColor Cyan

# 1) Namespace
kubectl apply -f (Join-Path $k8s "namespace.yaml")
if ($Namespace -ne "sistema-hospitalar") {
  Write-Host "Aviso: manifests usam namespace 'sistema-hospitalar'. Ajuste os YAML ou use o default." -ForegroundColor Yellow
}

# 2) ConfigMap (não sensível)
kubectl apply -f (Join-Path $k8s "configmap.yaml")

# 3) Secrets — obrigatório; documentação dos campos
$secretExists = $false
try {
  kubectl get secret hospital-secrets -n $Namespace 2>$null | Out-Null
  if ($LASTEXITCODE -eq 0) { $secretExists = $true }
} catch {
  $secretExists = $false
}

if (-not $secretExists) {
  Write-Host @"

Segredos necessários (Secret hospital-secrets no namespace $Namespace):
  ConnectionStrings__DefaultConnection   — string PostgreSQL
  ConnectionStrings__RabbitMQ              — amqp://...
  Jwt__Key                                 — chave JWT (>= 32 chars)
  FieldEncryption__Key                     — criptografia de campos (>= 32)
  FieldEncryption__HashKey                 — hash de CPF/campos (>= 32)

Exemplo:
  kubectl create secret generic hospital-secrets -n $Namespace ``
    --from-literal=ConnectionStrings__DefaultConnection='Host=postgres;Port=5432;Database=sistema_hospitalar;Username=postgres;Password=TROCAR' ``
    --from-literal=Jwt__Key='CHAVE-JWT-MINIMO-32-CARACTERES!!!!!!!!' ``
    --from-literal=FieldEncryption__Key='CHAVE-FIELD-ENCRYPTION-MIN-32!!!!!!' ``
    --from-literal=FieldEncryption__HashKey='CHAVE-FIELD-HASH-MIN-32!!!!!!!!!!' ``
    --from-literal=ConnectionStrings__RabbitMQ='amqp://guest:guest@rabbitmq:5672'

"@ -ForegroundColor Yellow

  if ($CreateExampleSecret) {
    Write-Host "Criando secret a partir de secret.example.yaml (lab apenas)..." -ForegroundColor Yellow
    kubectl apply -f (Join-Path $k8s "secret.example.yaml")
  } else {
    throw "Secret hospital-secrets ausente. Crie-o e rode novamente (ou use -CreateExampleSecret para lab)."
  }
}

# 4) Infra de dados
kubectl apply -f (Join-Path $k8s "postgres.yaml")
kubectl apply -f (Join-Path $k8s "redis.yaml")
kubectl apply -f (Join-Path $k8s "rabbitmq.yaml")

# 5) Aplicações
kubectl apply -f (Join-Path $k8s "api-deployment.yaml")
kubectl apply -f (Join-Path $k8s "web-deployment.yaml")
kubectl apply -f (Join-Path $k8s "ingress.yaml")

# 6) Overrides de imagem (opcional)
if ($ImageRegistry) {
  $ApiImage = if ($ApiImage) { $ApiImage } else { "$ImageRegistry/sistema-hospitalar-api:latest" }
  $WebImage = if ($WebImage) { $WebImage } else { "$ImageRegistry/sistema-hospitalar-web:latest" }
}

if ($ApiImage) {
  Write-Host "==> API image: $ApiImage" -ForegroundColor Cyan
  kubectl set image deployment/api "api=$ApiImage" -n $Namespace
}

if ($WebImage) {
  Write-Host "==> Web image: $WebImage" -ForegroundColor Cyan
  kubectl set image deployment/web "web=$WebImage" -n $Namespace
}

if (-not $SkipWait) {
  Write-Host "==> Aguardando rollouts..." -ForegroundColor Cyan
  kubectl rollout status deployment/postgres -n $Namespace --timeout=180s
  kubectl rollout status deployment/redis -n $Namespace --timeout=120s
  kubectl rollout status deployment/rabbitmq -n $Namespace --timeout=180s
  kubectl rollout status deployment/api -n $Namespace --timeout=300s
  kubectl rollout status deployment/web -n $Namespace --timeout=180s
}

Write-Host @"

Deploy concluído.
  kubectl get pods -n $Namespace
  Homologação (sem demo seeds): Hospital__EnableDemoSeeds=false (já no ConfigMap).
  Demo local em cluster: kubectl set env deployment/api Hospital__EnableDemoSeeds=true -n $Namespace

"@ -ForegroundColor Green
