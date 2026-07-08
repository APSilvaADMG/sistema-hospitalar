# Catálogo completo de regras de negócio — Sistema Hospitalar GTH

> **Levantamento:** 06/07/2026  
> **Fontes:** `BusinessRuleCatalog.cs`, classes `*Rules.cs` em `SistemaHospitalar.Domain/BusinessRules`, serviços de aplicação e documentação em `docs/business-rules/`.  
> **Consulta em runtime:** `GET /api/business-rules` (autenticado; permissão `reports.read`, `audit.read` ou `security.manage`).

---

## Legenda de status

| Status | Significado |
|--------|-------------|
| **Implementado** | Validação executável no backend; bloqueio ou alerta em fluxo real |
| **Parcial** | Regra existe mas cobertura incompleta ou depende de integração externa |
| **Planejado** | Catalogado no BRD; ainda sem validação automática |

Mensagens de bloqueio seguem o padrão **`[CÓDIGO] descrição em pt-BR`**.

---

## 1. Regra mestra (Core)

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| BR-CORE-001 | Exclusão lógica obrigatória | Dados clínicos, financeiros e operacionais não são removidos fisicamente — uso de `IsActive`, cancelamento ou estorno | Implementado | `BaseEntity`, todos os módulos |

---

## 2. Segurança e acesso

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| BR-SEG-001 | Controle de acesso por perfil | Permissões granulares por módulo (`PermissionCodes`) | Implementado | `Infrastructure/Security` |
| BR-SEG-002 | MFA para perfis críticos | Administradores e perfis sensíveis exigem autenticação multifator | Implementado | `AuthService` |
| BR-SEG-003 | Bloqueio por tentativas | 5 tentativas inválidas bloqueiam a conta | Implementado | `AuthService` |
| RN-025 / BR-LGPD-003 | Quebra de sigilo | Acesso a prontuário sem vínculo gera `SecurityIncident` e `AuditLog` | Implementado | `PhysicalAccessService` |

---

## 3. LGPD e privacidade

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| BR-LGPD-001 | Consentimento | Campos de consentimento e assinatura no cadastro de paciente | Implementado | Entidade `Patient` |
| BR-LGPD-002 | Log de acesso | `AuditLog` registra acessos e alterações sensíveis | Implementado | `AuditService` |
| BR-LGPD-003 | Quebra de sigilo | Tentativas de acesso negado geram incidente de segurança | Implementado | `SecurityIncident` |

---

## 4. Paciente e cadastro

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| RN-001 | CPF único | Dois pacientes ativos não podem compartilhar o mesmo CPF (hash `CpfHash`) | Implementado | `PatientCpfRules`, `PatientService` |
| RN-001b | CNS único | Dois pacientes ativos não podem compartilhar o mesmo CNS | Implementado | `HospitalBusinessRules.ValidateUniqueCns` |
| RN-002 | CNS obrigatório SUS | Atendimento SUS exige CNS com 15 dígitos e checksum válido | Implementado | `PatientCnsRules`, `HospitalBusinessRules.ValidateCnsForSus` |
| RN-003 | Menor de idade | Menor de 18 anos exige responsável (contato emergência ou CPF do responsável) | Implementado | `PatientRegistrationRules`, `HospitalBusinessRules.ValidateMinorHasResponsible` |
| RN-004 (PAC) | CPF obrigatório | CPF obrigatório no cadastro (exceto fluxo com CPF do responsável) | Implementado | `PatientRegistrationRules` |
| RN-005 | Responsável legal | Dados completos do responsável quando `UsesResponsibleCpf` | Implementado | `PatientRegistrationRules.ValidateLegalResponsible` |
| RN-006 | Responsável maior de idade | Responsável legal deve ter ≥ 18 anos | Implementado | `PatientRegistrationRules` |
| RN-003b | Autorização formal | Parentesco "Outro" exige termo de curatela, guarda ou procuração | Implementado | `PatientRegistrationRules` |
| RN-047 | Paciente falecido | Óbito registrado bloqueia novos atendimentos; histórico permanece consultável | Implementado | `HospitalBusinessRules.ValidateEligibleForCare` |
| BR-PAC-002 | Campos obrigatórios | Nome, nascimento, sexo, CPF (ou responsável), endereço e telefone no cadastro completo | Implementado | `PatientService` |
| RN-PAC-001 | Número de prontuário | Formato único `PAC0000000001` | Implementado | `PatientRecordNumberRules` |
| RN-PAC-025 | Sinal de duplicidade | Alerta de possível duplicidade cadastral | Parcial | `PatientService` |
| RN-GER-006 | Motivo de inativação | Inativar cadastro exige motivo informado | Implementado | `HospitalBusinessRules.ValidateInactivationReason` |
| RN-004b | Cadastro desatualizado 12 meses | Alertar cadastro sem atualização há mais de 12 meses | Planejado | Constante `CadastralUpdateMonths` definida; alerta não automatizado |

### Validações técnicas de documentos

- **CPF:** 11 dígitos, dígitos verificadores, rejeita sequências iguais (`PatientCpfRules`).
- **CNS:** 15 dígitos, soma ponderada mod 11 (`PatientCnsRules`).

---

## 5. Recepção, agendamento e atendimento

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| RN-004 | Abertura de atendimento | Paciente ativo, não falecido; convênio SUS validado; sem conflito de agenda | Implementado | `PatientCareValidation`, `AppointmentService` |
| RN-005 | Atendimento SUS | Validar CNS antes de agendamento, PS e internação quando convênio SUS | Implementado | `PatientCareValidation` |
| RN-006 | Atendimento particular | Conta a receber gerada ao concluir consulta particular | Implementado | `FinancialAccountService` |
| BR-AGE-001 / RN-AGD-008 | Conflito de horário | Profissional não pode ter dois agendamentos simultâneos | Implementado | `AppointmentService` |
| RN-CAN-001 | Cancelamento com justificativa | Cancelamento de agendamento ou teleconsulta exige motivo | Implementado | `AttendanceRules`, `AppointmentService` |
| RN-ATD-003 | Troca de paciente bloqueada | Não é permitido trocar paciente após início do atendimento | Implementado | `HospitalBusinessRules.ValidateCannotChangePatientAfterCareStarted` |
| RN-ATD-007 | Histórico de status | Alterações de status de atendimento auditadas | Implementado | `ClinicalStatusAuditLogger` |
| BR-AGE-002 | Confirmação WhatsApp | Ao criar agendamento, Connect envia confirmação via WhatsApp | Implementado | `ConnectService` |
| BR-AGE-003 | Lembrete automático | Lembretes 24h/48h via `ConnectReminderWorker` | Implementado | `ConnectReminderWorker` |
| RN-028–030 | Cancelamento por WhatsApp | Resposta "NÃO" libera vaga e notifica recepção | Implementado | `ConnectBotService` |
| BR-FAT-002 | Elegibilidade convênio | Convênios com `RequiresEligibilityCheck`: alerta se check > 24h; aviso se inelegível | Implementado | `AppointmentService.ValidateEligibilityAsync` |

---

## 6. Triagem e pronto-socorro

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| RN-007 | Triagem obrigatória | PS exige registro de triagem (`AiTriageLog`) antes de `InCare` para urgências Emergency/High | Implementado | `HospitalBusinessRules.ValidateTriageBeforeMedicalCare`, `EmergencyService` |
| RN-008 | Prioridade Manchester | Fila PS ordenada por urgência | Implementado | `EmergencyService` |
| BR-TRI-001 | Classificação de risco | Protocolo Manchester na IA assistencial e PS | Implementado | `AiService` |
| RN-TRI-004 | Sinais vitais imutáveis | Sinais vitais não podem ser sobrescritos — nova aferição com motivo | Parcial | `HospitalBusinessRules.ValidateVitalSignCorrectionAsNewEntry`, `BedsideCareService` |

### Parâmetros de SLA de espera (Manchester)

| Urgência | SLA máximo (minutos) |
|----------|----------------------|
| Emergency | 0 (imediato) |
| High | 10 |
| Medium | 60 |
| Low | 120 |
| NonUrgent | 240 |

- Violação de SLA calculada por `HospitalBusinessRules.IsEmergencyWaitExceeded`.
- Ocupação crítica de leitos: alerta quando taxa ≥ **90%** (`CriticalBedOccupancyPercent`).

---

## 7. Prontuário eletrônico (PEP)

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| RN-009 / BR-PEP-002 | Evolução imutável | Registro assinado não pode ser editado — apenas novo adendo | Implementado | `MedicalRecordService` |
| RN-010 / BR-PEP-003 | Auditoria clínica | Alterações geram `AuditLog` (usuário, IP, dispositivo, timestamp) | Implementado | `AuditService` |
| BR-PEP-001 | Autoria do registro | Evolução exige profissional, data/hora e assinatura digital opcional | Implementado | `MedicalRecordService` |
| BR-OFF-001 | Operação offline PEP | Evoluções com `ClientRequestId` e assinatura offline (`SyncMutations`) | Implementado | `MedicalRecordService` |
| BR-OFF-002 | Sincronização | `SyncMutations` com resolução idempotente ao reconectar | Implementado | `SyncService` |
| RN-GER-005 | Auditoria de alterações críticas | Mudanças sensíveis registradas em auditoria | Implementado | `AuditService` |

---

## 8. Prescrição e medicamentos

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| RN-011 | Prescritor habilitado | Prescrições vinculadas a `Professional` com CRM | Parcial | `MedicalRecordService` (permissões) |
| RN-012 | Interação medicamentosa | Alerta de interações e alergias | Parcial | `AiService.AnalyzePrescriptionSafetyAsync` |
| RN-013 | Medicamento controlado | CRM + assinatura + justificativa para controlados | Planejado | — |
| RN-PRE-006 / RN-012b | Checagem de alergia | Bloqueio se medicamento prescrito conflita com alergia registrada (termos ≥ 4 caracteres) | Implementado | `PrescriptionRules`, `MedicalRecordService`, `PharmacyService` |
| AI-RX-001 | Segurança de prescrição (IA) | Análise assistida de risco na prescrição | Implementado | `AiService`, `ClinicalIntelligenceService` |

---

## 9. Enfermagem e administração de medicamentos

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| RN-011b | Cinco certos | Validação paciente-medicamento-dose-via-horário (janela ±2h) | Implementado | `NursingRules`, `BedsideCareService` |
| RN-ADM-002 | Identificação do paciente | Confirmar identidade antes de administrar medicamento | Implementado | `HospitalBusinessRules.ValidateMedicationPatientIdentified`, `BedsideCareService` |

---

## 10. Farmácia

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| RN-014 | Baixa automática de estoque | Dispensação debita estoque e registra `StockMovement` | Implementado | `PharmacyService` |
| RN-015 | Estoque mínimo | Dashboard e BI alertam produtos abaixo do mínimo | Implementado | `DashboardService`, `WarehouseService` |
| RN-016 | Medicamento vencido | Lote vencido bloqueia dispensação | Implementado | `HospitalBusinessRules.ValidateMedicationNotExpired` |
| RN-023 | FEFO | Saída consome primeiro o lote com validade mais próxima | Implementado | `LotInventoryHelper`, `WarehouseService`, `InventoryService` |

---

## 11. Almoxarifado e materiais

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| RN-MAT-020 | Rastreabilidade por lote | Medicamentos exigem lote identificado em toda saída | Implementado | `WarehouseRules.ValidateLotTraceabilityForMedication` |
| RN-MAT-022 | Descartável sem devolução | Itens descartáveis não retornam ao estoque após consumo | Implementado | `WarehouseRules.ValidateDisposableNoReturn` |
| RN-MAT-025 | Auditoria cadastral | Alterações cadastrais e requisições registradas em `AuditLog` | Implementado | `WarehouseService`, `StockRequisitionService` |
| RN-023 | Estoque insuficiente | Bloqueio quando quantidade solicitada > saldo (produto ou lote) | Implementado | `HospitalBusinessRules.ValidateDispenseQuantity`, `WarehouseRules.ValidateLotQuantity` |
| RN-EST | Reposição preditiva | Sugestões de reposição com base em consumo e mínimo | Implementado | `ClinicalIntelligenceService` |

---

## 12. Internação e leitos

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| RN-017 | Status único de leito | Leito: Available, Reserved, Occupied, Cleaning, Maintenance | Implementado | `BedStatus` |
| RN-018 | Transferência | Transferência libera leito anterior, registra `BedTransfer` e atualiza censo | Implementado | `HospitalizationService` |
| RN-019 | Alta hospitalar | Alta encerra internação, libera leito, dispara higienização e eventos de faturamento | Implementado | `HospitalizationService` + `HospitalEventEngine` (`patient.discharged`) |
| RN-INT-003 | Uma internação ativa | Paciente não pode ter mais de uma internação ativa simultânea | Implementado | `HospitalBusinessRules.ValidateOneBedPerPatient`, `HospitalizationService` |
| RN-INT-006 | Leito ocupado bloqueado | Internação bloqueada em leito ocupado, em higienização ou manutenção | Implementado | `HospitalBusinessRules.ValidateOccupiedBedBlocked` |
| RN-ALT-004 | Pendências na alta | Alta bloqueada com prescrições abertas ou exames laboratoriais críticos pendentes | Implementado | `DischargeRules`, `HospitalizationService` |

---

## 13. Centro cirúrgico e CME

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| RN-020 | Checklist OMS | Cirurgia inicia somente com Sign In e Time Out completos | Implementado | `HospitalBusinessRules.ValidateOmsBeforeSurgeryStart`, `SurgeryService` |
| RN-021 | Consentimento | Sem consentimento confirmado, status InProgress bloqueado | Implementado | `HospitalBusinessRules` |
| BR-CC-002 | Reserva de sala | Conflito de horário e sala validado no agendamento cirúrgico | Implementado | `SurgeryService` |
| RN-021b | Kit estéril | Instrumental não esterilizado ou vencido não pode ser liberado | Implementado | `HospitalBusinessRules.ValidateSterileKit`, `CmeService` |
| RN-022 | Validade estéril | Kit com validade estéril vencida bloqueado | Implementado | `HospitalBusinessRules`, `CmeService` |
| RN-EST-001 | Ciclo único | Kit não pode ter dois ciclos de esterilização simultâneos | Implementado | `SterilizationRules` |
| RN-EST-002 | Kit em progresso | Kit já em esterilização não inicia novo ciclo | Implementado | `SterilizationRules` |
| RN-EST-004 | Ciclo reprovado | Ciclo reprovado não libera kit como estéril; rejeição explícita via API | Implementado | `SterilizationRules`, `CmeService.RejectSterilizationCycleAsync` |

---

## 14. Faturamento, TISS e convênios

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| RN-022 | Conta hospitalar | Fechamento exige itens auditados e prescrições/evoluções conferidas | Implementado | `TissBillingService` |
| RN-028 | Conta fechada TISS | Guia TISS só envia após conta fechada e todos os itens auditados | Implementado | `HospitalBusinessRules.ValidateTissGuideReadyForBilling` |
| BR-FAT-001 | Auditoria de conta | Conta deve estar fechada (`accountClosedAt`) antes de faturar | Implementado | `HospitalBusinessRules.ValidateBillingAccountClosed` |
| BR-FAT-002 | Elegibilidade convênio | Autorização TISS e cobertura validadas antes do faturamento | Implementado | `InsuranceIntegrationService` |
| BR-REG-001 | Produção SUS | Relatórios BPA, AIH, SIA, e-SUS e CNES no catálogo | Implementado | `ReportsService` |

---

## 15. Ambulância e transporte interno

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| RN-AMB-001 | Código único | Código de ambulância único na frota | Implementado | `AmbulanceRules` |
| RN-AMB-002 | Placa única | Placa única na frota | Implementado | `AmbulanceRules` |
| RN-AMB-008 | Manutenção bloqueia dispatch | Ambulância em manutenção ou indisponível não pode ser despachada | Implementado | `AmbulanceRules` |
| — | SLA transporte interno | Prazo: **10 min** (urgente) ou **30 min** (normal); flag `IsSlaViolated` | Implementado | `TransportService` |

---

## 16. Telemedicina

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| RN-TEL-001 | Agendamento futuro | Teleconsulta deve ser agendada para data/hora futura | Implementado | `TelemedicineRules` |
| RN-TEL-002 | Conclusão exige início | Status Completed exige `StartedAt` preenchido | Implementado | `TelemedicineRules` |
| RN-CAN-001 | Cancelamento | Cancelamento de teleconsulta exige justificativa | Implementado | `AttendanceRules` |

---

## 17. Equipamentos e fornecedores

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| RN-EQP-016 | Manutenção bloqueia reserva | Equipamento em manutenção ou fora de serviço não pode ser reservado | Implementado | `EquipmentRules` |
| RN-EQP-017 | Calibração vencida | Equipamento com calibração vencida bloqueado para uso clínico | Implementado | `EquipmentRules` |
| RN-FOR-010 | Fornecedor bloqueado | Fornecedor inativo ou bloqueado não participa de pedidos de compra | Implementado | `SupplierRules` |

---

## 18. Operação hospitalar (GTH 360)

| Área | Regra | Descrição | Status | Onde |
|------|-------|-----------|--------|------|
| Event engine | Alta → hotelaria | Evento `patient.discharged` dispara pendência de higienização de leito | Implementado | `HospitalEventEngine` |
| Event engine | Prescrição assinada | Evento `prescription.signed` registrado e roteado | Implementado | `HospitalEventEngine` |
| Event engine | Estoque baixo | Evento `stock.low` para alertas operacionais | Implementado | `HospitalEventEngine` |
| Task engine | Missões por papel | Missões (`PendingItem`) filtradas por perfil (recepção, enfermagem, almoxarifado, hotelaria) | Implementado | `TaskEngineService` |
| Resíduos | Classificação | Resíduos hospitalares por tipo (infectious, sharps, common, chemical, pharmaceutical) e status de coleta | Implementado | `WasteService` |

---

## 19. Inteligência clínica e IA

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| AI-TRI-001 | Triagem enriquecida | Triagem com histórico clínico do paciente | Implementado | `AiService` |
| AI-CID-001 | CID-10 assistido | Sugestão de CID ponderada + fallback Groq | Implementado | `AiService` |
| AI-RX-001 | Segurança de prescrição | Análise de alergias e riscos na prescrição | Implementado | `AiService` |
| AI-OPS-001 | Painel hospitalar | Alertas operacionais (ocupação, PS, estoque) | Implementado | `ClinicalIntelligenceService` |

---

## 20. BI e indicadores

| Código | Título | Descrição | Status | Onde |
|--------|--------|-----------|--------|------|
| BR-BI-001 | Indicadores automáticos | Ocupação, permanência, giro de leitos, mortalidade e produção | Implementado | `ReportsService`, `BiService`, `DashboardService` |

---

## 21. Financeiro e caixa (regras operacionais)

| Regra | Descrição | Onde |
|-------|-----------|------|
| Sessão de caixa única | Apenas um caixa aberto por vez | `FinancialCashSessionService` |
| Saldo esperado físico | Fundo de caixa + recebimentos em dinheiro − pagamentos em dinheiro no período | `FinancialCashSessionService` |
| Movimento por canal | Recepção (dinheiro/Pix/cartão) e receita operacional do dia calculados separadamente | `FinancialCashSessionService` |
| Contas a pagar | Categorias com valores de referência (fornecedor, folha, utilidades, impostos, manutenção) | `FinancialAccountService.GetPayableCategoryPresets` |
| Exclusão lógica financeira | Contas e pagamentos inativados via `IsActive`, não exclusão física | `FinancialAccount`, `FinancialPayment` |

---

## 22. Regras catalogadas mas ainda não implementadas

| Código | Módulo | Título |
|--------|--------|--------|
| RN-004b | Recepção | Alerta automático de cadastro desatualizado há mais de 12 meses |
| RN-011 | Prescrição | Validação completa de prescritor habilitado (CRM + especialidade) |
| RN-012 | Prescrição | Interação medicamentosa com bulário/Consulta Remédios integrado |
| RN-013 | Prescrição | Fluxo completo de medicamento controlado (receita especial, justificativa) |

---

## 23. Mapa de arquivos fonte

| Arquivo | Responsabilidade |
|---------|------------------|
| `BusinessRuleCatalog.cs` | Catálogo corporativo APSMedCore v1.0 (API + documentação) |
| `HospitalBusinessRules.cs` | Regras transversais: paciente, leito, estoque, triagem, cirurgia, faturamento |
| `PatientCpfRules.cs` | Validação e unicidade de CPF |
| `PatientCnsRules.cs` | Validação de CNS (checksum) |
| `PatientRegistrationRules.cs` | Cadastro, responsável legal, autorização |
| `PatientRecordNumberRules.cs` | Formato PAC0000000001 |
| `PrescriptionRules.cs` | Alergias na prescrição |
| `NursingRules.cs` | Cinco certos |
| `DischargeRules.cs` | Pendências na alta |
| `AttendanceRules.cs` | Cancelamento com justificativa |
| `WarehouseRules.cs` | Lote, FEFO, descartáveis |
| `AmbulanceRules.cs` | Frota e despacho |
| `TelemedicineRules.cs` | Teleconsulta |
| `SterilizationRules.cs` | CME / esterilização |
| `EquipmentRules.cs` | Equipamentos clínicos |
| `SupplierRules.cs` | Fornecedores |

---

## 24. Como manter este documento atualizado

1. Ao adicionar regra em `*Rules.cs`, registrar entrada em `BusinessRuleCatalog.cs` com `Implemented = true/false`.
2. Atualizar `docs/business-rules/audit.md` (matriz de auditoria) e este catálogo.
3. Validar via `GET /api/business-rules?implementedOnly=true`.
4. Mensagens de erro devem incluir o código entre colchetes para rastreabilidade.

---

*Documento gerado a partir do código-fonte do repositório sistema-hospitalar em 06/07/2026.*
