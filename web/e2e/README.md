# Testes E2E (Playwright)

Smoke tests dos fluxos críticos do frontend hospitalar.

## Pré-requisitos

1. API .NET rodando em `http://127.0.0.1:8080` (seed demo com `admin@hospital.local` / `Admin123!`)
2. Dependências instaladas: `npm install` em `web/`
3. Browsers Playwright (primeira vez): `npx playwright install chromium`

## Executar

Na pasta `web/`:

```bash
npx playwright test
```

Outros comandos úteis:

```bash
npm run test:e2e          # mesmo que acima
npm run test:e2e:ui       # interface interativa
npm run test:e2e:headed   # browser visível
```

## Variáveis de ambiente

| Variável | Padrão | Descrição |
|----------|--------|-----------|
| `PLAYWRIGHT_BASE_URL` | `http://127.0.0.1:5173` | URL do frontend |
| `PLAYWRIGHT_API_URL` | `http://127.0.0.1:8080` | URL da API (health check) |
| `PLAYWRIGHT_SKIP_WEBSERVER` | — | Defina `1` se o Vite já estiver rodando |
| `CI` | — | Desativa `reuseExistingServer` no webServer |

## O que é validado

- `GET /health` na API
- Login com usuário demo
- Sala de espera (`/emergencia`) — título e grid de KPIs (sem valores fixos)
- Hub financeiro (`/financeiro`) — cabeçalho e KPIs
- BI (`/bi`) — cabeçalho e grid de indicadores

Os testes não fixam números de KPI; apenas verificam que a página carregou estrutura essencial.
