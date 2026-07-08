# GTH 360 — Status de rotas e lacunas

Atualizado: 2026-07-06 · Legenda: **done** · **partial** · **placeholder**

## Painel e operação

| Rota | Menu | Status | Notas |
|------|------|--------|-------|
| `/` | sidebar, Feegow | done | DashboardPage + FeegowDashboard |
| `/dashboard/command-center` | sidebar | **done** | CommandCenterPage + eventos recentes |
| `/dashboard/assistencial` | sidebar, dashboardTabs | done | DashboardAssistencialPage |
| `/dashboard/tarefas` | dashboardTabs | done | DashboardTasksPage + missões API |
| `/pendencias` | sidebar | **done** | PendenciesPage + KPIs + seed demo |
| `/bi` | sidebar, dashboardTabs | done | BiPage |
| `/notificacoes` | dashboardTabs | done | NotificationsPage |

## Jornada clínica (sidebar)

| Rota | Status | Notas |
|------|--------|-------|
| `/recepcao` | done | ReceptionWorkspacePage |
| `/ambulatorio` | done | AmbulatoryWorkspacePage |
| `/emergencia` | done | EmergencyPage + abas |
| `/internacao` | partial | Hub + mapa leitos; transferências parciais |
| `/uti` | partial | IcuPage |
| `/centro-cirurgico` | partial | SurgeryPage |
| `/cme` | partial | CmePage |
| `/pep` | done | PepHubPage |
| `/enfermagem` | partial | NursingHubPage + PoC leito |
| `/ccih` | partial | InfectionControlPage |
| `/laboratorio` | partial | LaboratoryPage |
| `/imagem` | partial | ImagingPage |
| `/farmacia` | partial | PharmacyHubPage |
| `/hemoterapia` | partial | HemotherapyPage |
| `/nutricao` | partial | NutritionPage |
| `/oncologia` | partial | OncologyPage |

## Operação hospitalar

| Rota | Status | Notas |
|------|--------|-------|
| `/transportes` | partial | TransportHubPage + SLA badge |
| `/hotelaria` | partial | HotelariaHubPage + painel eventos |
| `/dialise` | partial | DialysisPage |
| `/fisioterapia` | partial | PhysiotherapyPage |
| `/lavanderia` | partial | LaundryPage |
| `/ambulancias` | partial | AmbulancePage |
| `/residuos` | **done** | WasteManagementPage + API CRUD + seed demo |

## Faturamento e financeiro

| Rota | Status | Notas |
|------|--------|-------|
| `/faturamento` | partial | SusBillingHubPage |
| `/faturamento-tiss` | partial | TissPage |
| `/financeiro` | partial | FinancialHubPage + Feegow workspace |
| `/convenios` | partial | HealthPlansPage |
| `/guias` | partial | GuidesHubPage |

## Feegow top menu (amostra)

| Rota | Status | Notas |
|------|--------|-------|
| `/recepcao/agendamentos/*` | done | Feegow agenda workspace |
| `/recepcao/pacientes/*` | done | Feegow patient workspace |
| `/estoque/dashboard` | done | FeegowWarehouseDashboard |
| `/estoque/entrada`, `/saida` | done | Receipt/Issue pages |
| `/estoque/requisicoes` | done | Stock requisition list |
| `/financeiro/contas-a-pagar/*` | partial | Hub financeiro Feegow |
| `/financeiro/caixas` | partial | FinancialHubPage |
| `/connect` | done | ConnectHubPage |

## API novos endpoints (esta entrega)

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/command-center/dashboard` | Agregado operacional |
| GET | `/api/patients/{id}/timeline` | Timeline 360° (50 eventos) |
| GET | `/api/events/recent` | Auditoria de eventos operacionais |
| GET | `/api/tasks/my-missions` | Missões por perfil |
| POST | `/api/tasks/{id}/complete` | Concluir missão (PendingItem) |
| GET/POST/PATCH | `/api/waste/*` | Resíduos hospitalares |
| POST | `/api/cme/cycles/{id}/reject` | Rejeitar ciclo de esterilização |

---

## Top 20 lacunas restantes (prioridade)

| # | Lacuna | Prioridade | Status |
|---|--------|------------|--------|
| 1 | Event engine (alta → hotelaria) | Alta | **done** | HospitalEventEngine in-process + log + RabbitMQ opcional |
| 2 | Resíduos hospitalares (`/residuos`) | Média | **done** | WasteCollection + WasteManagementPage |
| 3 | Lavanderia produção/distribuição completa | Média | partial |
| 4 | CME rastreabilidade cirúrgica | Média | **partial** | RejectSterilizationCycle + link CC |
| 5 | Nutrição cozinha/distribuição | Média | partial |
| 6 | Transporte interno SLA maqueiros | Média | **partial** | SlaDeadlineAt + badge UI |
| 7 | Convênios elegibilidade no atendimento | Alta | **partial** | EligibilityPanel + warn on agendamento |
| 8 | Índices DB + paginação listas críticas | Alta | planned |
| 9 | PWA offline enfermagem | Baixa | planned |
| 10 | Certificado ICP-Brasil A1/A3 | Baixa | planned |
| 11 | Integração PACS real | Média | partial |
| 12 | RFID pulseira | Baixa | planned |
| 13 | Multi-unidade / rede | Baixa | planned |
| 14 | IA preditiva operacional | Baixa | planned |
| 15 | Rouparia hotelaria completa | Média | planned |
| 16 | Feegow financeiro (TEF, cheques, cartões) | Média | placeholder |
| 17 | Relatórios analíticos Feegow | Média | partial |
| 18 | Task engine missões por perfil | Alta | **done** | TaskEngineService + DashboardTasksPage API |
| 19 | Pesquisa global unificada | Média | partial |
| 20 | Assinatura eletrônica todos tipos PEP | Alta | partial |
