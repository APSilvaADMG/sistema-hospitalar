# Módulo Almoxarifado

Documentação do módulo de almoxarifado (estoque central) do Sistema Hospitalar, alinhado ao layout Feegow.

## Visão geral

O almoxarifado gerencia entradas por nota fiscal, saídas por setor, rastreabilidade por lote (FEFO) e requisições internas. Integra-se ao cadastro de produtos (`/estoque`) e à farmácia por ala.

## Permissões

| Código | Descrição |
|--------|-----------|
| `warehouse.manage` | Gerenciar almoxarifado (entradas, saídas, dashboard, negar requisições) |
| `pharmacy.dispense` | Criar/visualizar requisições de estoque |

## API — `/api/warehouse`

Todos os endpoints exigem autenticação JWT e permissão `warehouse.manage`.

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/dashboard` | KPIs: produtos, estoque baixo, lotes a vencer, movimentos do dia, requisições pendentes |
| GET | `/lots?productId=&expiringWithinDays=` | Listagem de lotes com saldo |
| GET | `/expiring?days=30` | Lotes próximos do vencimento |
| GET | `/low-stock` | Produtos abaixo do mínimo |
| GET | `/consumption-by-sector?from=&to=` | Consumo agregado por setor |
| POST | `/receipts` | Entrada de NF com itens e lotes |
| POST | `/issues` | Saída para setor (consumo, perda, transferência, paciente) |

## API — Requisições (`/api/inventory/requisitions`)

| Método | Rota | Descrição |
|--------|------|-----------|
| PATCH | `/{id}/deny` | Negar requisição (corpo: `{ "reason": "..." }`) — status `Denied` |

Fluxo de requisição: **Pendente → Aprovada → Atendida** ou **Negada/Cancelada**.

## Regras de negócio

| Código | Regra |
|--------|-------|
| RN-MAT-020 | Medicamentos exigem rastreabilidade por lote em saídas |
| RN-MAT-022 | Itens descartáveis não podem retornar ao estoque (devolução bloqueada) |
| RN-023 (FEFO) | Saídas consomem primeiro o lote com validade mais próxima |

Implementação: `WarehouseRules`, `LotInventoryHelper`, aplicadas em `WarehouseService` e no atendimento de requisições.

## Telas (frontend)

| Rota | Componente | Função |
|------|------------|--------|
| `/estoque/dashboard` | `FeegowWarehouseDashboard` | Painel operacional |
| `/estoque/entrada` | `FeegowStockReceiptForm` | Entrada NF |
| `/estoque/saida` | `FeegowStockIssueForm` | Saída por setor |
| `/estoque/requisicoes` | `FeegowStockRequisitionList` | Listagem com ação Negar |

## Banco de dados

Tabelas criadas pela migration `AddWarehouseLotsAndReceipts`:

- `estoque_lotes` — `ProductLot`
- `estoque_entradas` / `estoque_entrada_itens` — recebimentos NF
- `estoque_saidas` / `estoque_saida_itens` — saídas

## Serviços

- `IWarehouseService` / `WarehouseService` — operações do almoxarifado
- `IStockRequisitionService` — requisições (inclui `DenyRequisitionAsync` e FEFO no fulfill)

Registro DI: `services.AddScoped<IWarehouseService, WarehouseService>();`
