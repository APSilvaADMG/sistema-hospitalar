using System.Globalization;
using SistemaHospitalar.Application.DTOs.Reports;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Reports;

/// <summary>Rótulos em português brasileiro para valores exibidos em relatórios.</summary>
internal static class ReportLabels
{
    private static readonly HashSet<string> LocalizableRowKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "status", "type", "urgency", "role", "shift", "direction", "category", "severity", "class", "metric", "item",
    };

    private static readonly Dictionary<string, Dictionary<string, string>> ColumnSpecificTokens =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["urgency"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Low"] = "Verde — pouco urgente",
                ["Medium"] = "Amarelo — urgente",
                ["High"] = "Laranja — muito urgente",
                ["Emergency"] = "Vermelho — emergência",
                ["NonUrgent"] = "Azul — não urgente",
            },
            ["severity"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Low"] = "Baixa",
                ["Moderate"] = "Moderada",
                ["High"] = "Alta",
                ["Severe"] = "Grave",
            },
            ["type"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["AccessDenied"] = "Acesso negado",
                ["VisitorIssue"] = "Problema com visitante",
                ["AssetAlert"] = "Alerta de patrimônio",
                ["Emergency"] = "Emergência",
                ["PatientFall"] = "Queda de paciente",
                ["MedicationError"] = "Erro de medicação",
                ["ClinicalAdverseEvent"] = "Evento adverso clínico",
                ["NearMiss"] = "Quase-erro",
                ["Urinary"] = "Trato urinário",
                ["Respiratory"] = "Respiratória",
                ["SurgicalSite"] = "Sítio cirúrgico",
                ["Bloodstream"] = "Corrente sanguínea",
                ["Inbound"] = "Entrada",
                ["Outbound"] = "Saída",
                ["Adjustment"] = "Ajuste",
                ["Anamnesis"] = "Anamnese",
                ["Evolution"] = "Evolução",
                ["Prescription"] = "Prescrição",
                ["ExamRequest"] = "Solicitação de exame",
                ["Procedure"] = "Procedimento",
            },
        };

    private static readonly Dictionary<string, string> Tokens = new(StringComparer.OrdinalIgnoreCase)
    {
        // AppointmentStatus / SurgeryStatus / Lab / Imaging / Chemo / Dialysis (valores compartilhados)
        ["Scheduled"] = "Agendado",
        ["Confirmed"] = "Confirmado",
        ["InProgress"] = "Em andamento",
        ["Completed"] = "Realizado",
        ["Cancelled"] = "Cancelado",
        ["NoShow"] = "Falta",
        ["Requested"] = "Solicitado",
        ["InPreparation"] = "Em preparo",
        ["Administered"] = "Administrado",

        // BedStatus / OperatingRoomStatus
        ["Available"] = "Disponível",
        ["Occupied"] = "Ocupado",
        ["Maintenance"] = "Manutenção",
        ["Cleaning"] = "Higienização",
        ["Reserved"] = "Reservado",
        ["InUse"] = "Em uso",

        // EmergencyVisitStatus
        ["Waiting"] = "Aguardando",
        ["InCare"] = "Em atendimento",
        ["Discharged"] = "Alta",
        ["Referred"] = "Encaminhado",

        // StockMovementType
        ["Inbound"] = "Entrada",
        ["Outbound"] = "Saída",
        ["Adjustment"] = "Ajuste",

        // FinancialAccountStatus / Cash session
        ["Open"] = "Em aberto",
        ["PartiallyPaid"] = "Parcialmente pago",
        ["Paid"] = "Pago",
        ["Closed"] = "Fechado",

        // FinancialAccountDirection
        ["Receivable"] = "Receita",
        ["Payable"] = "Despesa",

        // FinancialAccountCategory
        ["Consultation"] = "Consulta",
        ["Hospitalization"] = "Internação",
        ["Exam"] = "Exame",
        ["Copayment"] = "Coparticipação",
        ["Parking"] = "Estacionamento",
        ["Other"] = "Outro",
        ["SupplierPurchase"] = "Compra de fornecedor",
        ["Payroll"] = "Folha de pagamento",
        ["Utilities"] = "Utilidades",
        ["Taxes"] = "Impostos",
        ["OtherExpense"] = "Outra despesa",
        ["InsuranceReceivable"] = "Recebível de convênio",

        // EmployeeRole
        ["Nurse"] = "Enfermagem",
        ["Technician"] = "Técnico",
        ["Administrative"] = "Administrativo",
        ["Manager"] = "Gestão",

        // ShiftType
        ["Morning"] = "Manhã",
        ["Afternoon"] = "Tarde",
        ["Night"] = "Noite",

        // InfectionSurveillanceStatus
        ["Suspected"] = "Suspeita",
        ["Confirmed"] = "Confirmada",
        ["Resolved"] = "Resolvida",

        // SecurityIncidentStatus
        ["Investigating"] = "Em investigação",

        // TissGuideStatus
        ["Draft"] = "Rascunho",
        ["Sent"] = "Enviada",
        ["Glosa"] = "Glosada",

        // GlosaContestationStatus
        ["None"] = "Nenhum",
        ["Submitted"] = "Enviado",
        ["Accepted"] = "Aceito",
        ["Rejected"] = "Rejeitado",

        // HospitalizationStatus
        ["Active"] = "Ativa",
        ["Transferred"] = "Transferida",

        // ImagingModality
        ["XRay"] = "Raio-X",
        ["CT"] = "Tomografia",
        ["MRI"] = "Ressonância magnética",
        ["Ultrasound"] = "Ultrassom",
        ["Mammography"] = "Mamografia",

        // KPI / colunas genéricas
        ["Status"] = "Situação",
        ["Total"] = "Total",
    };

    private static readonly Dictionary<string, string> KpiLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Status"] = "Situação",
        ["Total cases (n)"] = "Total de casos (n)",
        ["Deaths in period (n)"] = "Óbitos no período (n)",
        ["Total doses (n)"] = "Total de doses (n)",
    };

    public static string Translate(string? value, string? columnKey = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(columnKey)
            && ColumnSpecificTokens.TryGetValue(columnKey, out var columnMap)
            && columnMap.TryGetValue(value, out var columnLabel))
        {
            return columnLabel;
        }

        if (Tokens.TryGetValue(value, out var label))
        {
            return label;
        }

        if (Enum.TryParse<DayOfWeek>(value, true, out var dow))
        {
            return CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetDayName(dow);
        }

        return value;
    }

    public static string? TranslateSubtitle(string? subtitle) =>
        string.IsNullOrWhiteSpace(subtitle) ? subtitle : Translate(subtitle);

    public static List<Dictionary<string, object?>> LocalizeRows(
        IReadOnlyList<Dictionary<string, object?>> rows,
        IReadOnlyList<ReportColumnDto> columns)
    {
        var keys = columns
            .Select(c => c.Key)
            .Where(k => LocalizableRowKeys.Contains(k))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (keys.Count == 0)
        {
            return rows.ToList();
        }

        return rows.Select(row =>
        {
            var copy = new Dictionary<string, object?>(row);
            foreach (var key in keys)
            {
                if (!copy.TryGetValue(key, out var raw) || raw is null)
                {
                    continue;
                }

                copy[key] = Translate(raw.ToString(), key);
            }

            return copy;
        }).ToList();
    }

    public static IReadOnlyList<ReportKpiDto> LocalizeKpis(IReadOnlyList<ReportKpiDto> kpis) =>
        kpis.Select(k => new ReportKpiDto(
            KpiLabels.TryGetValue(k.Label, out var label) ? label : Translate(k.Label),
            k.Value,
            k.Variant)).ToList();

    public static string AppointmentStatusLabel(AppointmentStatus status) => Translate(status.ToString());
    public static string BedStatusLabel(BedStatus status) => Translate(status.ToString());
    public static string SurgeryStatusLabel(SurgeryStatus status) => Translate(status.ToString());
    public static string LabOrderStatusLabel(LabOrderStatus status) => Translate(status.ToString());
    public static string ImagingStudyStatusLabel(ImagingStudyStatus status) => Translate(status.ToString());
    public static string EmergencyVisitStatusLabel(EmergencyVisitStatus status) => Translate(status.ToString());
    public static string TriageUrgencyLabel(TriageUrgency urgency) => Translate(urgency.ToString());
    public static string OperatingRoomStatusLabel(OperatingRoomStatus status) => Translate(status.ToString());
    public static string StockMovementTypeLabel(StockMovementType type) => Translate(type.ToString());
    public static string FinancialAccountStatusLabel(FinancialAccountStatus status) => Translate(status.ToString());
    public static string FinancialAccountDirectionLabel(FinancialAccountDirection direction) => Translate(direction.ToString());
    public static string FinancialAccountCategoryLabel(FinancialAccountCategory category) => Translate(category.ToString());
    public static string MedicalRecordEntryTypeLabel(MedicalRecordEntryType type) => Translate(type.ToString());
    public static string EmployeeRoleLabel(EmployeeRole role) => Translate(role.ToString());
    public static string ShiftTypeLabel(ShiftType shift) => Translate(shift.ToString());
    public static string InfectionTypeLabel(InfectionType type) => Translate(type.ToString());
    public static string InfectionSurveillanceStatusLabel(InfectionSurveillanceStatus status) => Translate(status.ToString());
    public static string SecurityIncidentTypeLabel(SecurityIncidentType type) => Translate(type.ToString());
    public static string ClinicalIncidentSeverityLabel(ClinicalIncidentSeverity? severity) =>
        severity.HasValue ? Translate(severity.Value.ToString()) : "—";
    public static string SecurityIncidentStatusLabel(SecurityIncidentStatus status) => Translate(status.ToString());
    public static string TissGuideStatusLabel(TissGuideStatus status) => Translate(status.ToString());
    public static string GlosaContestationStatusLabel(GlosaContestationStatus status) => Translate(status.ToString());
    public static string FinancialCashSessionStatusLabel(FinancialCashSessionStatus status) => Translate(status.ToString());
    public static string ChemotherapySessionStatusLabel(ChemotherapySessionStatus status) => Translate(status.ToString());
    public static string DialysisSessionStatusLabel(DialysisSessionStatus status) => Translate(status.ToString());
    public static string ImagingModalityLabel(ImagingModality modality) => Translate(modality.ToString());
    public static string HospitalizationStatusLabel(HospitalizationStatus status) => Translate(status.ToString());
}
