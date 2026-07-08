namespace SistemaHospitalar.Domain.BusinessRules;

public record BusinessRuleDefinition(
    string Code,
    string Module,
    string Title,
    string Description,
    bool Implemented,
    string? BrReference = null,
    string? Layer = null);

/// <summary>
/// Catálogo corporativo APSMedCore v1.0 — alinhado ao BRD hospitalar + SUS + LGPD.
/// Regras executadas em Domain/Infrastructure; frontend apenas exibe.
/// </summary>
public static class BusinessRuleCatalog
{
    public static IReadOnlyList<BusinessRuleDefinition> All { get; } =
    [
        // ── Regra mestra ──
        new("BR-CORE-001", "Core", "Exclusão lógica obrigatória",
            "Dados clínicos, financeiros e operacionais não são removidos fisicamente — IsActive, cancelamento ou estorno.",
            true, null, "Domain/BaseEntity"),

        // ── M01 Segurança ──
        new("BR-SEG-001", "Segurança", "Controle de acesso por perfil",
            "Permissões granulares por módulo (PermissionCodes) — recepção, clínico, farmácia, auditoria.",
            true, "RN-024", "Infrastructure/Security"),
        new("BR-SEG-002", "Segurança", "MFA para perfis críticos",
            "Administradores e perfis sensíveis exigem autenticação multifator.",
            true, "RN-039", "Infrastructure/Security"),
        new("BR-SEG-003", "Segurança", "Bloqueio por tentativas",
            "5 tentativas inválidas bloqueiam a conta.",
            true, "RN-040", "Infrastructure/Security"),
        new("RN-025", "Segurança", "Quebra de sigilo",
            "Acesso a prontuário sem vínculo gera evento em SecurityIncident e AuditLog.",
            true, "BR-LGPD-003", "Infrastructure/PhysicalAccess"),

        // ── M02 Paciente ──
        new("RN-001", "Pacientes", "CPF único",
            "Não permitir dois pacientes ativos com o mesmo CPF (hash CpfHash).",
            true, "BR-PAC-001", "Domain/PatientCpfRules"),
        new("RN-001b", "Pacientes", "CNS único",
            "Não permitir dois pacientes ativos com o mesmo CNS (hash CnsHash).",
            true, "BR-PAC-001", "Domain/HospitalBusinessRules"),
        new("RN-002", "Pacientes", "CNS obrigatório SUS",
            "Atendimento SUS exige CNS válido (15 dígitos + checksum).",
            true, "BR-PAC-003", "Domain/PatientCnsRules"),
        new("RN-003", "Pacientes", "Menor de idade",
            "Menor de 18 anos exige responsável (contato emergência ou mãe). CPF responsável quando UsesResponsibleCpf.",
            true, "BR-PAC-002", "Domain/PatientRegistrationRules"),
        new("RN-047", "Pacientes", "Paciente falecido",
            "Óbito registrado bloqueia novos atendimentos; histórico permanece consultável.",
            true, "RN-003", "HospitalBusinessRules.ValidateEligibleForCare"),
        new("BR-PAC-002", "Pacientes", "Campos obrigatórios",
            "Nome, nascimento, sexo, CPF (ou responsável), endereço e telefone no cadastro completo.",
            true, "RN-004", "Application/PatientService"),

        // ── M03 Recepção / Agendamento ──
        new("RN-004", "Recepção", "Abertura de atendimento",
            "Paciente ativo, não falecido; convênio SUS validado quando aplicável; conflito de agenda bloqueado.",
            true, "BR-AGE-001", "PatientCareValidation"),
        new("RN-005", "Recepção", "Atendimento SUS",
            "Validar CNS antes de agendamento, PS e internação quando convênio SUS.",
            true, "RN-005", "PatientCareValidation"),
        new("RN-006", "Recepção", "Atendimento particular",
            "Conta a receber gerada via FinancialAccountService ao concluir consulta particular.",
            true, "RN-006", "FinancialAccountService"),
        new("BR-AGE-001", "Agendamento", "Conflito de horário",
            "Profissional não pode ter dois agendamentos simultâneos.",
            true, "RN-004", "AppointmentService"),
        new("BR-AGE-002", "Agendamento", "Confirmação WhatsApp",
            "Ao criar agendamento, Connect envia confirmação via WhatsApp.",
            true, "RN-028", "Connect/ConnectService"),
        new("BR-AGE-003", "Agendamento", "Lembrete automático",
            "Lembretes 24h/48h via ConnectReminderWorker.",
            true, "RN-029", "Connect/ConnectReminderWorker"),
        new("RN-030", "Agendamento", "Cancelamento por WhatsApp",
            "Resposta NÃO libera vaga e notifica recepção.",
            true, "RN-030", "Connect/ConnectBotService"),

        // ── M04 Triagem ──
        new("RN-007", "Triagem", "Sinais vitais / triagem",
            "PS exige registro de triagem (AiTriageLog) antes de InCare para urgências altas.",
            true, "BR-TRI-001", "EmergencyService"),
        new("RN-008", "Triagem", "Prioridade Manchester",
            "Fila PS ordenada por urgência; SLA por cor (0/10/60/120/240 min).",
            true, "BR-TRI-002", "EmergencyService + AiService"),
        new("BR-TRI-001", "Triagem", "Classificação de risco",
            "Protocolo Manchester implementado na IA assistencial e PS.",
            true, "RN-007", "AiService"),

        // ── M05 Prontuário ──
        new("RN-009", "Prontuário", "Evolução imutável",
            "Registro assinado não pode ser editado — apenas novo adendo.",
            true, "BR-PEP-002", "MedicalRecordService"),
        new("RN-010", "Prontuário", "Auditoria clínica",
            "Alterações geram AuditLog (usuário, IP, dispositivo, timestamp).",
            true, "BR-PEP-003", "AuditService"),
        new("BR-PEP-001", "Prontuário", "Autoria do registro",
            "Evolução exige profissional, data/hora e assinatura digital opcional.",
            true, "RN-009", "MedicalRecordService"),

        // ── M06 Prescrição ──
        new("RN-011", "Prescrição", "Prescritor habilitado",
            "Prescrições vinculadas a Professional com CRM — perfil médico/dentista via permissões.",
            false, "BR-MED-001", "MedicalRecordService"),
        new("RN-012", "Prescrição", "Interação medicamentosa",
            "Alerta de interações e alergias — integração bulário/consulta remédios planejada.",
            false, "BR-MED-002", "—"),
        new("RN-013", "Prescrição", "Medicamento controlado",
            "CRM + assinatura + justificativa para controlados.",
            false, "BR-MED-003", "—"),

        // ── M07 Farmácia ──
        new("RN-014", "Farmácia", "Baixa automática estoque",
            "Dispensação debita estoque e registra StockMovement.",
            true, "BR-FAR-001", "PharmacyService"),
        new("RN-015", "Farmácia", "Estoque mínimo",
            "Dashboard e BI alertam produtos abaixo do mínimo; compras automáticas via PurchasingService.",
            true, "BR-FAR-002", "DashboardService"),
        new("RN-016", "Farmácia", "Medicamento vencido",
            "Lote vencido bloqueia dispensação de medicamentos.",
            true, "BR-FAR-003", "HospitalBusinessRules"),
        new("RN-023", "Farmácia", "FEFO",
            "Saída consome primeiro o lote com validade mais próxima (FEFO).",
            true, "BR-FAR-001", "WarehouseService + InventoryService"),

        // ── M07 Almoxarifado ──
        new("RN-MAT-020", "Almoxarifado", "Rastreabilidade por lote",
            "Medicamentos exigem lote identificado em toda saída.",
            true, "BR-FAR-003", "Domain/WarehouseRules"),
        new("RN-MAT-022", "Almoxarifado", "Descartável sem devolução",
            "Itens descartáveis não podem retornar ao estoque após consumo (entrada com motivo devolução bloqueada).",
            true, "BR-FAR-001", "Domain/WarehouseRules"),
        new("RN-MAT-025", "Almoxarifado", "Auditoria cadastral",
            "Alterações cadastrais e atendimento de requisições registradas em AuditLog.",
            true, "RN-010", "WarehouseService + StockRequisitionService"),

        // ── M08 Internação ──
        new("RN-017", "Internação", "Status único de leito",
            "Leito: Available, Reserved, Occupied, Maintenance (higienização/interditado).",
            true, "BR-INT-001", "Domain/BedStatus"),
        new("RN-018", "Internação", "Transferência",
            "Transferência libera leito anterior, registra BedTransfer e atualiza censo.",
            true, "BR-INT-002", "HospitalizationService"),
        new("RN-019", "Internação", "Alta hospitalar",
            "Alta encerra internação, libera leito, dispara higienização e faturamento.",
            true, "BR-INT-002", "HospitalizationService"),

        // ── M09 Centro Cirúrgico ──
        new("RN-020", "Centro Cirúrgico", "Checklist OMS",
            "Cirurgia inicia somente com Sign In e Time Out completos.",
            true, "BR-CC-001", "SurgeryService"),
        new("RN-021", "Centro Cirúrgico", "Consentimento",
            "Sem consentimento confirmado, status InProgress bloqueado.",
            true, "BR-CC-001", "HospitalBusinessRules"),
        new("BR-CC-002", "Centro Cirúrgico", "Reserva de sala",
            "Conflito de horário e sala validado no agendamento cirúrgico.",
            true, "RN-021", "SurgeryService"),

        // ── M10 Faturamento ──
        new("RN-022", "Faturamento", "Conta hospitalar",
            "Fechamento exige itens auditados e prescrições/evoluções conferidas.",
            true, "BR-FAT-001", "TissBillingService"),
        new("RN-028", "Faturamento", "Conta fechada TISS",
            "Guia TISS só envia após conta fechada e itens auditados.",
            true, "BR-FAT-001", "HospitalBusinessRules"),
        new("BR-FAT-002", "Faturamento", "Elegibilidade convênio",
            "Autorização TISS e cobertura validadas antes do faturamento.",
            true, "RN-023", "InsuranceIntegrationService"),

        // ── M11 LGPD ──
        new("BR-LGPD-001", "LGPD", "Consentimento",
            "Campos de consentimento e assinatura no cadastro de paciente.",
            true, "RN-024", "Patient entity"),
        new("BR-LGPD-002", "LGPD", "Log de acesso",
            "AuditLog registra acessos e alterações sensíveis.",
            true, "RN-010", "AuditService"),
        new("BR-LGPD-003", "LGPD", "Quebra de sigilo",
            "Tentativas de acesso negado geram SecurityIncident.",
            true, "RN-025", "PhysicalAccessService"),

        // ── M12 Offline ──
        new("BR-OFF-001", "Offline", "Operação offline PEP",
            "Evoluções com ClientRequestId e assinatura offline (SyncMutations).",
            true, "RN-026", "MedicalRecordService"),
        new("BR-OFF-002", "Offline", "Sincronização",
            "SyncMutations com resolução idempotente ao reconectar.",
            true, "RN-027", "Infrastructure/Sync"),

        // ── M13 BI ──
        new("BR-BI-001", "BI", "Indicadores automáticos",
            "Ocupação, permanência, giro de leitos, mortalidade e produção via ReportsService.",
            true, "RN-019", "ReportsService"),

        // ── CME / esterilização ──
        new("RN-021b", "CME", "Kit estéril",
            "Instrumental não esterilizado ou vencido não pode ser liberado.",
            true, "BR-CC-002", "CmeService"),

        // ── Regulatório ──
        new("BR-REG-001", "Regulatório", "Produção SUS",
            "Relatórios BPA, AIH, SIA, e-SUS e CNES no catálogo de relatórios.",
            true, "RN-005", "ReportsService"),

        // ── Pendentes prioritários ──
        new("RN-004b", "Recepção", "Atualização cadastral 12 meses",
            "Alertar cadastro desatualizado há mais de 12 meses.",
            false, "BR-PAC-002", "—"),
        new("RN-011b", "Enfermagem", "Cinco certos",
            "Validação paciente-medicamento-dose-via-horário na administração.",
            true, "BR-MED-001", "BedsideCareService + NursingRules"),
        new("RN-012b", "Prescrição", "Alergia medicamentosa",
            "Bloquear prescrição se medicamento consta em alergias do paciente.",
            true, "BR-MED-001", "PrescriptionRules + MedicalRecordService"),
        new("RN-PRE-006", "Prescrição", "Checagem de alergia",
            "Validação de alergias antes de prescrever ou dispensar.",
            true, "RN-012b", "PrescriptionRules"),
        new("RN-ADM-002", "Enfermagem", "Identificação do paciente",
            "Confirmar identidade antes de administrar medicamento.",
            true, "RN-011b", "BedsideCareService"),
        new("RN-INT-003", "Internação", "Uma internação ativa",
            "Paciente não pode ter mais de uma internação ativa simultânea.",
            true, "BR-INT-001", "HospitalizationService"),
        new("RN-CAN-001", "Atendimento", "Justificativa de cancelamento",
            "Cancelamento de agendamento exige motivo.",
            true, "RN-ATD-007", "AttendanceRules + AppointmentService"),
        new("RN-ALT-004", "Internação", "Pendências na alta",
            "Alta bloqueada com prescrições abertas ou labs críticos pendentes.",
            true, "BR-INT-002", "DischargeRules + HospitalizationService"),
    ];
}
