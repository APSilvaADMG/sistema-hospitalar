# Inteligência Clínica e Operacional

Camada de apoio à decisão do APSMedCore — combina **regras determinísticas** (sem LLM) com **enriquecimento opcional via Groq** quando configurado.

## Serviços

### `ClinicalIntelligenceService` (`/api/clinical-intelligence`)

| Endpoint | Função | Tipo |
|---|---|---|
| `GET .../patients/{id}/alerts` | Alergias, problemas ativos, vitais críticos, prescrições pendentes | Regras |
| `GET .../stock/replenishment` | Itens abaixo do mínimo, consumo médio 30d, dias até ruptura | Regras + estatística |
| `GET .../operational` | PS, ocupação de leitos, estoque baixo, lotes a vencer | Regras |

### `AiService` (`/api/ai`)

| Recurso | Função | Tipo |
|---|---|---|
| `POST /triage` | Manchester + histórico do paciente (alergias, PS) | Regras + histórico DB |
| `POST /cid10/suggest` | Score ponderado no catálogo CID-10 | Regras; Groq se score zero |
| `POST /prescription/safety` | Alergias + terapia duplicada + stub interações | Regras |
| `GET /insights/hospital-dashboard` | Resumo operacional para gestão | Regras + Groq opcional |
| Insights existentes | Surto, recorrência, triagem operacional | Agregados + Groq opcional |

## Regra vs LLM

- **Regras (`PrescriptionRules`, `NursingRules`, `WarehouseRules`, etc.)** — bloqueiam operações com `[RN-XXX]`; sempre executadas.
- **Heurísticas IA** — alertas informativos (severidade critical/warning/info); não substituem julgamento clínico.
- **Groq** — enriquece relatórios markdown quando `Groq:Enabled=true` e API key configurada; nunca envia PII identificável em prompts de surto/epidemiologia.

## Frontend

- Painel **Alertas clínicos (IA)** no resumo do paciente Feegow (`FeegowClinicalAlertsPanel`).
- Seção **Sugestões de reposição (IA)** no dashboard do almoxarifado.
- Métodos em `web/src/api/client.ts`: `getPatientClinicalAlerts`, `getStockReplenishmentSuggestions`, `analyzePrescriptionSafety`, etc.

## Configuração Groq

```json
"Groq": {
  "Enabled": true,
  "ApiKey": "...",
  "Model": "llama-3.3-70b-versatile"
}
```

Sem Groq, o sistema permanece funcional com relatórios baseados em dados do banco.
