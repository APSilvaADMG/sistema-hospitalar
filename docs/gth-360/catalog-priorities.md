# Catálogo prioritizado (resumo)

Legenda: **Status** — `done` implementado · `partial` parcial · `planned` planejado

## 🟢 Essencial — Clínico e identificação

| Módulo | Funcionalidade | Status | Notas |
|--------|----------------|--------|-------|
| Cadastro | Pacientes completos | done | Feegow + recepção |
| Cadastro | Usuários e perfis demo | done | 10 perfis seed |
| Atendimento | Triagem PS | partial | Emergency module |
| PEP | Evolução, prescrição, exames | done | Seções Feegow |
| PEP | Assinatura eletrônica + senha | partial | Fila global; expandir tipos |
| PEP | Bloqueio pós-assinatura | done | MedicalRecordService |
| Identificação | Pulseira GTH + QR | done | PatientIdentity |
| Identificação | Etiquetas exame/med/amostra | done | GenerateLabel |
| Identificação | Impressão pulseira/etiqueta | done | patientIdentityPrint |
| Identificação | Scan → abrir paciente | done | Recepção + leito |
| Leito | Point of Care (vitals) | done | /api/bedside/vitals |
| Leito | eMAR com prescrição assinada | partial | Administração + senha |
| Internação | Leitos e altas | partial | WardDetails seed |
| Farmácia | Dispensação básica | partial | Ward pharmacy |
| Faturamento | Contas e TISS guias | partial | TissController |
| Auditoria | Trilha assinaturas | done | PepSignaturesController |

## 🟡 Importante — Operação hospitalar

| Módulo | Funcionalidade | Status |
|--------|----------------|--------|
| Hotelaria | Limpeza pós-alta | partial |
| Hotelaria | Lavanderia / rouparia | planned |
| CME | Esterilização / kits | partial |
| Nutrição | Dietas e cozinha | partial |
| Maqueiros | Transporte interno | partial |
| Resíduos | Coleta e manifesto | partial |
| Convênios | Elegibilidade e guias | partial |
| SUS | Produção e envio | partial |
| TV / Totem | Painéis e senhas | done |
| Connect | WhatsApp mock | done |
| Performance | Seed 10k + k6 | done |
| Performance | Índices e paginação | planned |
| UX | Central de pendências | planned |
| UX | Timeline 360° paciente | planned |
| UX | Pesquisa global | partial |

## 🔵 Futuro — Plataforma 360°

| Módulo | Funcionalidade | Status |
|--------|----------------|--------|
| Event Engine | ALTA_PACIENTE → tarefas | planned |
| Task Engine | Missões por perfil | planned |
| Command Center | Painel operacional RT | partial |
| Mapa hospital | Leitos em tempo real | partial |
| Integrações | ICP-Brasil A1/A3 | planned |
| Integrações | RFID pulseira | planned |
| Mobile | PWA offline enfermagem | planned |
| Rede | Multi-hospital | planned |
| IA | Assistente operacional | planned |

## Benchmark ClinicCare (só ideias)

| Área | Aproveitar | Ignorar |
|------|------------|---------|
| Agendamento | Fluxo rápido, menos cliques | Stack Python |
| Dashboard | KPIs executivos | Modelo de dados |
| Prontuário | Navegação histórico | Layout copiado |
| Relatórios | Filtros e exportação | — |
