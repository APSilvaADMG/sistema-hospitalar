# Roadmap GTH 360 Enterprise

## v1.0 — Núcleo hospitalar (atual → consolidar)

**Objetivo:** hospital digital ponta a ponta sem quebrar o existente.

- [x] Docker + PostgreSQL + API React
- [x] PEP Feegow + assinaturas básicas
- [x] Pulseiras GTH + etiquetas + impressão
- [x] Atendimento no leito (PoC) + eMAR inicial
- [x] Massa de dados 10k (testes de carga)
- [ ] Índices DB + paginação listas críticas
- [ ] Eventos mínimos (alta → hotelaria)

**Entrega:** piloto clínico (1 enfermaria + recepção + PEP).

## v1.5 — Operação integrada

- Central de pendências / Minha Jornada por perfil
- Timeline 360° do paciente
- Mapa de leitos com cores
- Hotelaria: limpeza terminal automatizada pós-alta
- Convênios: fila autorizações + guias no fluxo

## v2.0 — Operação completa

- CME rastreabilidade cirúrgica
- Nutrição produção/distribuição
- Transporte interno (maqueiros) com SLA
- Resíduos hospitalares
- Command Center (ocupação, filas, cirurgias)
- Motor de eventos + tarefas

## v3.0 — Ecossistema

- Certificado digital ICP-Brasil
- Integrações SUS/convênio/lab/PACS
- PWA offline enfermagem
- Rede multi-unidade
- Indicadores preditivos (opcional)

## Ordem de desenvolvimento recomendada (próximos 90 dias)

| Sprint | Foco |
|--------|------|
| 1 | Performance busca paciente + índices |
| 2 | Event engine (alta, exame, assinatura) |
| 3 | Central pendências + timeline paciente |
| 4 | Hotelaria pós-alta automática |
| 5 | Convênios — elegibilidade no atendimento |
| 6 | Command Center MVP |

Cada sprint exige: build API + `npm run typecheck` + smoke manual dos fluxos Feegow/TV/login.
