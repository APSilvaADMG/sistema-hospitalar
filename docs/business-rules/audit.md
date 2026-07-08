# Auditoria de Regras de Negócio — APSMedCore

> Atualizado na sessão de finalização do almoxarifado e inteligência clínica.  
> Fontes: `BusinessRuleCatalog.cs`, `HospitalBusinessRules.cs`, `WarehouseRules.cs`, `PrescriptionRules.cs`, módulos ESF (RN-PAC, RN-ATD, RN-PRE, RN-ADM, RN-INT, RN-MAT, RN-AMB, RN-TEL, RN-EST).

Legenda de status: **Implementado** | **Parcial** | **Planejado**

---

## Geral

| ID | Módulo | Título | Status | Serviço/Arquivo |
|---|---|---|---|---|
| BR-CORE-001 | Geral | Exclusão lógica obrigatória | Implementado | `BaseEntity` |
| RN-GER-005 | Geral | Auditoria de alterações críticas | Implementado | `AuditService` |
| RN-GER-006 | Geral | Motivo de inativação | Implementado | `HospitalBusinessRules` |
| RN-004b | Recepção | Cadastro desatualizado 12 meses | Planejado | — |

---

## Paciente

| ID | Módulo | Título | Status | Serviço/Arquivo |
|---|---|---|---|---|
| RN-001 | Paciente | CPF único | Implementado | `PatientCpfRules` |
| RN-001b | Paciente | CNS único | Implementado | `HospitalBusinessRules` |
| RN-002 | Paciente | CNS obrigatório SUS | Implementado | `PatientCnsRules` |
| RN-003 | Paciente | Menor de idade / responsável | Implementado | `PatientRegistrationRules` |
| RN-047 | Paciente | Paciente falecido | Implementado | `HospitalBusinessRules.ValidateEligibleForCare` |
| BR-PAC-002 | Paciente | Campos obrigatórios cadastro | Implementado | `PatientService` |
| RN-PAC-001 | Paciente | Número de prontuário | Implementado | `PatientRecordNumberRules` |
| RN-PAC-025 | Paciente | Sinal de duplicidade | Parcial | `PatientService` |

---

## Atendimento

| ID | Módulo | Título | Status | Serviço/Arquivo |
|---|---|---|---|---|
| RN-004 | Recepção | Abertura de atendimento | Implementado | `PatientCareValidation` |
| RN-005 | Recepção | Atendimento SUS | Implementado | `PatientCareValidation` |
| RN-006 | Recepção | Atendimento particular | Implementado | `FinancialAccountService` |
| RN-ATD-003 | Atendimento | Troca de paciente bloqueada | Implementado | `HospitalBusinessRules` |
| RN-ATD-007 | Atendimento | Histórico de status | Implementado | `ClinicalStatusAuditLogger` |
| RN-CAN-001 | Atendimento | Cancelamento com justificativa | Implementado | `AttendanceRules` + `AppointmentService` |
| BR-AGE-001 | Agendamento | Conflito de horário | Implementado | `AppointmentService` |
| RN-028–030 | Agendamento | WhatsApp confirmação/lembrete | Implementado | `ConnectService` |

---

## Triagem

| ID | Módulo | Título | Status | Serviço/Arquivo |
|---|---|---|---|---|
| RN-007 | Triagem | Triagem obrigatória PS | Implementado | `EmergencyService` |
| RN-008 | Triagem | Prioridade Manchester | Implementado | `EmergencyService` + `AiService` |
| BR-TRI-001 | Triagem | Classificação de risco | Implementado | `AiService` |
| RN-TRI-004 | Triagem | Sinais vitais imutáveis | Parcial | `BedsideCareService` |

---

## Prescrição

| ID | Módulo | Título | Status | Serviço/Arquivo |
|---|---|---|---|---|
| RN-011 | Prescrição | Prescritor habilitado | Parcial | `MedicalRecordService` (permissões) |
| RN-012 | Prescrição | Interação medicamentosa | Parcial | `AiService.AnalyzePrescriptionSafetyAsync` (stub) |
| RN-013 | Prescrição | Medicamento controlado | Planejado | — |
| RN-PRE-006 | Prescrição | Checagem de alergia | Implementado | `PrescriptionRules` |
| RN-012b | Prescrição | Alergia medicamentosa | Implementado | `MedicalRecordService` + `PharmacyService` |

---

## Farmácia / Almoxarifado

| ID | Módulo | Título | Status | Serviço/Arquivo |
|---|---|---|---|---|
| RN-014 | Farmácia | Baixa automática estoque | Implementado | `PharmacyService` |
| RN-015 | Farmácia | Estoque mínimo | Implementado | `DashboardService` + `WarehouseService` |
| RN-016 | Farmácia | Medicamento vencido | Implementado | `HospitalBusinessRules` |
| RN-023 | Farmácia | FEFO | Implementado | `LotInventoryHelper` + `WarehouseService` |
| RN-MAT-020 | Almoxarifado | Rastreabilidade por lote | Implementado | `WarehouseRules` + `InventoryService` |
| RN-MAT-022 | Almoxarifado | Descartável sem devolução | Implementado | `WarehouseRules` + `InventoryService` |
| RN-MAT-025 | Almoxarifado | Auditoria cadastral | Implementado | `WarehouseService` |
| RN-EST | Almoxarifado | Reposição preditiva | Implementado | `ClinicalIntelligenceService` |

---

## Internação

| ID | Módulo | Título | Status | Serviço/Arquivo |
|---|---|---|---|---|
| RN-017 | Internação | Status único de leito | Implementado | `BedStatus` |
| RN-018 | Internação | Transferência | Implementado | `HospitalizationService` |
| RN-019 | Internação | Alta hospitalar | Implementado | `HospitalizationService` |
| RN-INT-003 | Internação | Uma internação ativa | Implementado | `HospitalizationService` |
| RN-INT-006 | Internação | Leito ocupado bloqueado | Implementado | `HospitalBusinessRules` |
| RN-ALT-004 | Internação | Pendências na alta | Implementado | `DischargeRules` |

---

## Cirurgia

| ID | Módulo | Título | Status | Serviço/Arquivo |
|---|---|---|---|---|
| RN-020 | Centro Cirúrgico | Checklist OMS | Implementado | `SurgeryService` |
| RN-021 | Centro Cirúrgico | Consentimento | Implementado | `HospitalBusinessRules` |
| BR-CC-002 | Centro Cirúrgico | Reserva de sala | Implementado | `SurgeryService` |
| RN-021b | CME | Kit estéril | Implementado | `CmeService` |

---

## PEP (Prontuário)

| ID | Módulo | Título | Status | Serviço/Arquivo |
|---|---|---|---|---|
| RN-009 | PEP | Evolução imutável | Implementado | `MedicalRecordService` |
| RN-010 | PEP | Auditoria clínica | Implementado | `AuditService` |
| BR-PEP-001 | PEP | Autoria do registro | Implementado | `MedicalRecordService` |
| BR-OFF-001/002 | PEP | Offline / sync | Implementado | `MedicalRecordService` + `SyncService` |

---

## Enfermagem / Administração

| ID | Módulo | Título | Status | Serviço/Arquivo |
|---|---|---|---|---|
| RN-ADM-002 | Enfermagem | ID paciente antes de medicação | Implementado | `BedsideCareService` |
| RN-011b | Enfermagem | Cinco certos | Implementado | `NursingRules` + `BedsideCareService` |

---

## Segurança

| ID | Módulo | Título | Status | Serviço/Arquivo |
|---|---|---|---|---|
| BR-SEG-001 | Segurança | Controle de acesso | Implementado | `PermissionAuthorization` |
| BR-SEG-002 | Segurança | MFA perfis críticos | Implementado | `AuthService` |
| BR-SEG-003 | Segurança | Bloqueio por tentativas | Implementado | `AuthService` |
| RN-025 | Segurança | Quebra de sigilo | Implementado | `PhysicalAccessService` |

---

## LGPD

| ID | Módulo | Título | Status | Serviço/Arquivo |
|---|---|---|---|---|
| BR-LGPD-001 | LGPD | Consentimento | Implementado | Entidade `Patient` |
| BR-LGPD-002 | LGPD | Log de acesso | Implementado | `AuditService` |
| BR-LGPD-003 | LGPD | Quebra de sigilo | Implementado | `SecurityIncident` |

---

## Ambulância / Telemedicina / Esterilização

| ID | Módulo | Título | Status | Serviço/Arquivo |
|---|---|---|---|---|
| RN-AMB-001/002 | Ambulância | Código/placa únicos | Implementado | `AmbulanceRules` |
| RN-AMB-008 | Ambulância | Manutenção bloqueia dispatch | Implementado | `AmbulanceRules` |
| RN-TEL-001/002 | Telemedicina | Agendamento e início sessão | Implementado | `TelemedicineRules` |
| RN-EST-001–004 | Esterilização | Ciclo único / kit em progresso | Implementado | `SterilizationRules` |

---

## Inteligência clínica (camada IA)

| ID | Módulo | Título | Status | Serviço/Arquivo |
|---|---|---|---|---|
| AI-TRI-001 | IA | Triagem enriquecida com histórico | Implementado | `AiService` |
| AI-CID-001 | IA | CID-10 ponderado + Groq fallback | Implementado | `AiService` |
| AI-RX-001 | IA | Segurança de prescrição | Implementado | `AiService` |
| AI-OPS-001 | IA | Painel hospitalar | Implementado | `AiService` + `ClinicalIntelligenceService` |
