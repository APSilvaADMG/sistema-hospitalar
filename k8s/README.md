# Kubernetes — Sistema Hospitalar

Manifests para deploy em cluster (Minikube, AKS, GKE, etc.).

Namespace: **`sistema-hospitalar`**

## Pré-requisitos

- `kubectl` apontando para o cluster
- Imagens disponíveis no cluster ou em registry:
  - Local/Minikube: `sistema-hospitalar-api:local` / `sistema-hospitalar-web:local`
  - GHCR (APSilvaADMG): `ghcr.io/apsilvaadmg/sistema-hospitalar-api:latest` e `...-web:latest`
- Secret `hospital-secrets` criado (veja abaixo) — **não** versionamos senhas reais

## Segredos obrigatórios

| Chave | Uso |
|-------|-----|
| `ConnectionStrings__DefaultConnection` | PostgreSQL |
| `ConnectionStrings__RabbitMQ` | RabbitMQ (opcional se não usar filas) |
| `Jwt__Key` | Assinatura JWT (≥ 32 caracteres) |
| `FieldEncryption__Key` | Criptografia de campos sensíveis |
| `FieldEncryption__HashKey` | Hash de CPF / busca |

```powershell
kubectl create secret generic hospital-secrets -n sistema-hospitalar `
  --from-literal=ConnectionStrings__DefaultConnection='Host=postgres;Port=5432;Database=sistema_hospitalar;Username=postgres;Password=TROCAR' `
  --from-literal=Jwt__Key='CHAVE-JWT-MINIMO-32-CARACTERES!!!!!!!!' `
  --from-literal=FieldEncryption__Key='CHAVE-FIELD-ENCRYPTION-MIN-32!!!!!!' `
  --from-literal=FieldEncryption__HashKey='CHAVE-FIELD-HASH-MIN-32!!!!!!!!!!' `
  --from-literal=ConnectionStrings__RabbitMQ='amqp://guest:guest@rabbitmq:5672'
```

Há um modelo em `secret.example.yaml` (valores `REPLACE_ME`) — use só em lab.

## Deploy rápido

```powershell
# Recomendado: script aplica manifests na ordem e documenta secrets
.\scripts\deploy-k8s.ps1

# Minikube com imagens locais
docker compose build api web
# (carregue no Minikube / tague como ...:local)
.\scripts\deploy-k8s.ps1 -ApiImage sistema-hospitalar-api:local -WebImage sistema-hospitalar-web:local

# Registry GHCR
.\scripts\deploy-k8s.ps1 -ImageRegistry "ghcr.io/apsilvaadmg"
```

Ordem dos manifests:

1. `namespace.yaml`
2. `configmap.yaml`
3. Secret `hospital-secrets` (manual)
4. `postgres.yaml` → `redis.yaml` → `rabbitmq.yaml`
5. `api-deployment.yaml` → `web-deployment.yaml` → `ingress.yaml`

## Homologação (sem seeds de demonstração)

O ConfigMap define `Hospital__EnableDemoSeeds=false` e `ASPNETCORE_ENVIRONMENT=Production`.

Para reativar demo no cluster (não recomendado em prod):

```powershell
kubectl set env deployment/api Hospital__EnableDemoSeeds=true -n sistema-hospitalar
```

## Acesso (Minikube)

```bash
minikube service web -n sistema-hospitalar
minikube service api -n sistema-hospitalar
```

Ingress: host `hospital.local` (ajuste DNS / `/etc/hosts`).

## Produção / AKS

1. Publique imagens no ACR/GHCR e atualize `image:` nos deployments (ou use `-ImageRegistry` / `-ApiImage`).
2. Prefira PostgreSQL gerenciado em vez do `postgres.yaml` no cluster.
3. Configure Ingress com TLS (cert-manager / Application Gateway).
4. Mantenha `Hospital__EnableDemoSeeds=false`.

## Estrutura

| Arquivo | Descrição |
|---------|-----------|
| `namespace.yaml` | Namespace `sistema-hospitalar` |
| `configmap.yaml` | Variáveis não sensíveis (incl. EnableDemoSeeds) |
| `secret.example.yaml` | Modelo de Secret (sem valores reais) |
| `postgres.yaml` | PostgreSQL + PVC |
| `redis.yaml` | Redis |
| `rabbitmq.yaml` | RabbitMQ |
| `api-deployment.yaml` | API ASP.NET Core |
| `web-deployment.yaml` | Frontend nginx |
| `ingress.yaml` | Roteamento `/api` e `/` |
