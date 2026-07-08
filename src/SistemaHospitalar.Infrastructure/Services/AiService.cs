using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;
using SistemaHospitalar.Application.DTOs.Ai;
using SistemaHospitalar.Application.DTOs.ClinicalIntelligence;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Ai;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class AiService(
    AppDbContext dbContext,
    IGroqLlmService groqLlm,
    IOptions<GroqOptions> groqOptions) : IAiService
{
    private readonly GroqOptions _groq = groqOptions.Value;    private static readonly (string[] Keywords, TriageUrgency Urgency)[] SymptomRules =
    [
        (["parada cardíaca", "parada cardiaca", "inconsciente", "desmaio", "convulsão", "convulsao",
            "sangramento intenso", "hemorragia", "infarto", "avc", "rebaixamento", "não respira", "nao respira"],
            TriageUrgency.Emergency),
        (["dor no peito", "falta de ar", "dispneia", "confusão", "confusao", "dor abdominal intensa",
            "trauma grave", "queimadura extensa", "fratura exposta"],
            TriageUrgency.High),
        (["febre alta", "vômito persistente", "vomito persistente", "diarreia intensa", "dor forte",
            "cefaleia intensa", "crise", "alergia"],
            TriageUrgency.Medium),
        (["tosse", "febre leve", "dor leve", "gripe", "resfriado", "náusea", "nausea", "corte superficial"],
            TriageUrgency.Low),
        (["check-up", "rotina", "receita", "renovação", "renovacao", "atestado", "orientação", "orientacao",
            "consulta eletiva", "retorno programado"],
            TriageUrgency.NonUrgent)
    ];

    public async Task<TriageResponse> AnalyzeTriageAsync(
        TriageRequest request, Guid? userId, CancellationToken cancellationToken = default)
    {
        var enrichedSymptoms = await EnrichSymptomsWithPatientHistoryAsync(request, cancellationToken);
        var text = enrichedSymptoms.Trim().ToLowerInvariant();
        var urgency = ClassifyUrgency(text, request);
        var meta = ManchesterMetadata(urgency);
        var specialty = RecommendedSpecialty(urgency, text);

        var cidSuggestions = await SuggestCid10InternalAsync(text, 3, cancellationToken);
        var topCid = cidSuggestions.FirstOrDefault();

        var assessmentNotes = BuildAssessmentNotes(request);

        var log = new AiTriageLog
        {
            PatientId = request.PatientId,
            UserId = userId,
            Symptoms = enrichedSymptoms.Trim(),
            Urgency = urgency,
            RecommendedSpecialty = specialty,
            SuggestedCid10 = topCid?.Code,
            SuggestedCid10Description = topCid?.Description,
            Notes = $"{meta.Guidance} | {assessmentNotes}"
        };

        dbContext.AiTriageLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new TriageResponse(
            log.Id,
            urgency,
            meta.Label,
            meta.Color,
            meta.ColorHex,
            meta.MaxWaitMinutes,
            meta.Referral,
            meta.ReferralLabel,
            specialty,
            topCid?.Code,
            topCid?.Description,
            meta.Guidance,
            cidSuggestions);
    }

    public async Task<IReadOnlyList<Cid10SuggestionDto>> SuggestCid10Async(
        Cid10SuggestionRequest request, CancellationToken cancellationToken = default)
    {
        return await SuggestCid10InternalAsync(request.Text, request.MaxResults, cancellationToken);
    }

    public async Task<TriageAdmissionSuggestionDto?> GetAdmissionSuggestionForPatientAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        var triage = await FindAvailableTriageForAdmissionAsync(patientId, null, cancellationToken);
        return triage is null ? null : TriageAdmissionHelper.ToSuggestionDto(triage);
    }

    public async Task<IReadOnlyList<AiTriageLogDto>> GetRecentTriageLogsAsync(
        int limit, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);

        var logs = await dbContext.AiTriageLogs
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .Select(l => new
            {
                l.Id,
                PatientName = l.Patient != null ? l.Patient.FullName : null,
                l.Symptoms,
                l.Urgency,
                l.RecommendedSpecialty,
                l.SuggestedCid10,
                l.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return logs
            .Select(l =>
            {
                var meta = ManchesterMetadata(l.Urgency);
                return new AiTriageLogDto(
                    l.Id,
                    l.PatientName,
                    l.Symptoms,
                    l.Urgency,
                    meta.Label,
                    meta.Color,
                    meta.MaxWaitMinutes,
                    l.RecommendedSpecialty,
                    l.SuggestedCid10,
                    l.CreatedAt);
            })
            .ToList();
    }

    public async Task<AiInsightReportDto> AnalyzeOutbreakAsync(
        int days, Guid? userId, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 7, 90);
        var since = DateTime.UtcNow.AddDays(-days);

        var infections = await dbContext.InfectionSurveillances.AsNoTracking()
            .CountAsync(i => i.DetectedAt >= since, cancellationToken);
        var respiratoryInfections = await dbContext.InfectionSurveillances.AsNoTracking()
            .CountAsync(i => i.DetectedAt >= since && i.InfectionType == InfectionType.Respiratory, cancellationToken);
        var erRespiratory = await dbContext.EmergencyVisits.AsNoTracking()
            .CountAsync(e => e.ArrivedAt >= since && (
                EF.Functions.ILike(e.ChiefComplaint, "%tosse%")
                || EF.Functions.ILike(e.ChiefComplaint, "%dispneia%")
                || EF.Functions.ILike(e.ChiefComplaint, "%respirat%")
                || EF.Functions.ILike(e.ChiefComplaint, "%febre%")), cancellationToken);
        var triageRespiratory = await dbContext.AiTriageLogs.AsNoTracking()
            .CountAsync(t => t.CreatedAt >= since && (
                EF.Functions.ILike(t.Symptoms, "%tosse%")
                || EF.Functions.ILike(t.Symptoms, "%dispneia%")
                || EF.Functions.ILike(t.Symptoms, "%respirat%")), cancellationToken);

        var last7 = DateTime.UtcNow.AddDays(-7);
        var recentInfections = await dbContext.InfectionSurveillances
            .CountAsync(i => i.DetectedAt >= last7, cancellationToken);

        var risk = recentInfections >= 5 || respiratoryInfections >= 3
            ? "Alerta"
            : recentInfections >= 2 || respiratoryInfections >= 1
                ? "Atenção"
                : "Normal";

        var indicators = new List<AiInsightIndicatorDto>
        {
            new("Infecções hospitalares (período)", infections.ToString(), infections > 0 ? "warning" : null),
            new("Infecções respiratórias", respiratoryInfections.ToString(), respiratoryInfections > 0 ? "warning" : null),
            new("Atendimentos PS — queixa respiratória", erRespiratory.ToString(), null),
            new("Triagens IA — sintomas respiratórios", triageRespiratory.ToString(), null),
            new("Casos últimos 7 dias", recentInfections.ToString(), recentInfections >= 2 ? "danger" : null),
        };

        var recommendations = risk switch
        {
            "Alerta" => "- Acionar CCIH e revisar isolamento respiratório.\n- Reforçar notificação compulsória.\n- Avaliar leitos de isolamento disponíveis.",
            "Atenção" => "- Monitorar curva epidemiológica diariamente.\n- Reforçar higienização de mãos e EPI.",
            _ => "- Situação estável. Manter vigilância rotineira da CCIH."
        };

        var markdown = $"""
            ## Relatório de surto respiratório (análise agregada)

            **Período:** últimos {days} dias · **Nível:** {risk}

            ### Indicadores
            {string.Join("\n", indicators.Select(i => $"- **{i.Label}:** {i.Value}"))}

            ### Recomendações
            {recommendations}

            _Dados anonimizados — nenhum dado pessoal enviado a modelos externos._
            """;

        var (finalMarkdown, groqEnriched) = await EnrichWithGroqAsync(
            """
            Você é epidemiologista hospitalar (CCIH). Analise indicadores AGREGADOS de surto respiratório.
            Responda em português, markdown, seções: Diagnóstico situacional, Hipóteses, Ações imediatas (máx 5 bullets).
            Não invente números — use apenas os fornecidos.
            """,
            $"""
            Período: {days} dias. Nível de risco calculado: {risk}.
            Infecções hospitalares: {infections}. Respiratórias: {respiratoryInfections}.
            PS queixa respiratória: {erRespiratory}. Triagens IA respiratórias: {triageRespiratory}.
            Casos últimos 7 dias: {recentInfections}.
            """,
            markdown,
            cancellationToken);

        var summary = $"Risco {risk}: {respiratoryInfections} infecção(ões) respiratória(s), {recentInfections} caso(s) na última semana.";
        return await SaveInsightAsync(
            AiInsightType.Outbreak,
            "Surto respiratório — vigilância epidemiológica",
            summary,
            risk,
            indicators,
            finalMarkdown,
            null,
            null,
            userId,
            groqEnriched,
            cancellationToken);
    }

    public async Task<AiInsightReportDto> AnalyzeRecurrentPatientAsync(
        Guid patientId, Guid? userId, CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Paciente não encontrado.");

        var since = DateTime.UtcNow.AddMonths(-12);
        var appointments = await dbContext.Appointments.CountAsync(
            a => a.PatientId == patientId && a.IsActive && a.ScheduledAt >= since
                && a.Status == AppointmentStatus.Completed, cancellationToken);
        var erVisits = await dbContext.EmergencyVisits.CountAsync(
            e => e.PatientId == patientId && e.IsActive && e.ArrivedAt >= since, cancellationToken);
        var hospitalizations = await dbContext.Hospitalizations.CountAsync(
            h => h.PatientId == patientId && h.IsActive && h.AdmittedAt >= since, cancellationToken);
        var total = appointments + erVisits + hospitalizations;

        var topComplaints = await dbContext.EmergencyVisits.AsNoTracking()
            .Where(e => e.PatientId == patientId && e.ArrivedAt >= since)
            .GroupBy(e => e.ChiefComplaint)
            .Select(g => new { Complaint = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(3)
            .ToListAsync(cancellationToken);

        var isRecurrent = total >= 5;
        var risk = total >= 10 ? "Alerta" : isRecurrent ? "Atenção" : "Normal";

        var indicators = new List<AiInsightIndicatorDto>
        {
            new("Consultas (12 meses)", appointments.ToString(), null),
            new("Passagens pelo PS", erVisits.ToString(), erVisits >= 3 ? "warning" : null),
            new("Internações", hospitalizations.ToString(), hospitalizations >= 2 ? "warning" : null),
            new("Total de contatos assistenciais", total.ToString(), isRecurrent ? "warning" : null),
        };

        var pattern = topComplaints.Count > 0
            ? string.Join(", ", topComplaints.Select(c => $"{c.Complaint} ({c.Count}x)"))
            : "Sem padrão identificado no PS";

        var suggestions = isRecurrent
            ? "- Avaliar plano de cuidado longitudinal e adesão terapêutica.\n- Considerar encaminhamento à Atenção Primária ou especialista.\n- Revisar medicações crônicas no prontuário."
            : "- Frequência dentro do esperado para acompanhamento ambulatorial.";

        var markdown = $"""
            ## Análise de paciente recorrente

            **Paciente:** {patient.FullName} · **Período:** 12 meses · **Nível:** {risk}

            ### Frequência
            {string.Join("\n", indicators.Select(i => $"- **{i.Label}:** {i.Value}"))}

            ### Padrões no PS
            {pattern}

            ### Sugestões clínicas
            {suggestions}
            """;

        var (finalMarkdown, groqEnriched) = await EnrichWithGroqAsync(
            """
            Você é médico assistente. Analise padrão de recorrência assistencial (sem identificar o paciente pelo nome).
            Responda em português, markdown: Interpretação, Possíveis causas, Condutas sugeridas (máx 5 bullets).
            Use apenas os dados fornecidos.
            """,
            $"""
            ID interno: {patientId}. Período: 12 meses. Nível: {risk}.
            Consultas: {appointments}. PS: {erVisits}. Internações: {hospitalizations}. Total: {total}.
            Queixas frequentes PS: {pattern}.
            """,
            markdown,
            cancellationToken);

        var summary = isRecurrent
            ? $"Paciente recorrente: {total} contatos em 12 meses."
            : $"Frequência habitual: {total} contato(s) em 12 meses.";

        return await SaveInsightAsync(
            AiInsightType.RecurrentPatient,
            $"Recorrência — {patient.FullName}",
            summary,
            risk,
            indicators,
            finalMarkdown,
            patientId,
            patient.FullName,
            userId,
            groqEnriched,
            cancellationToken);
    }

    public async Task<AiInsightReportDto> AnalyzeTriageOperationalAsync(
        int days, Guid? userId, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 1, 30);
        var since = DateTime.UtcNow.AddDays(-days);

        var visits = await dbContext.EmergencyVisits.AsNoTracking()
            .Where(e => e.IsActive && e.ArrivedAt >= since)
            .Select(e => new { e.Urgency, e.Status, e.ArrivedAt, e.StartedAt })
            .ToListAsync(cancellationToken);

        var waiting = visits.Count(v => v.Status == EmergencyVisitStatus.Waiting);
        var served = visits.Count(v => v.StartedAt.HasValue);
        var waitData = visits.Where(v => v.StartedAt.HasValue)
            .Select(v => (v.StartedAt!.Value - v.ArrivedAt).TotalMinutes)
            .ToList();
        var avgWait = waitData.Count == 0 ? 0 : Math.Round(waitData.Average(), 1);

        var byUrgency = visits.GroupBy(v => v.Urgency)
            .Select(g => new AiInsightIndicatorDto(
                $"Urgência {g.Key}",
                g.Count().ToString(),
                g.Key is TriageUrgency.Emergency or TriageUrgency.High ? "warning" : null))
            .ToList();

        var overload = waiting >= 10 || avgWait > 60;
        var risk = waiting >= 20 || avgWait > 90 ? "Alerta" : overload ? "Atenção" : "Normal";

        var indicators = new List<AiInsightIndicatorDto>
        {
            new("Atendimentos no período", visits.Count.ToString(), null),
            new("Aguardando atendimento", waiting.ToString(), waiting >= 5 ? "warning" : null),
            new("Tempo médio de espera (min)", avgWait.ToString(CultureInfo.InvariantCulture), avgWait > 60 ? "danger" : null),
        };
        indicators.AddRange(byUrgency);

        var recommendations = risk switch
        {
            "Alerta" => "- Abrir protocolo de superlotação do PS.\n- Reforçar equipe de enfermagem e acolhimento.\n- Priorizar casos vermelho/laranja.",
            "Atenção" => "- Monitorar fila a cada 30 minutos.\n- Avaliar remanejamento de profissionais.",
            _ => "- Fluxo operacional dentro dos parâmetros."
        };

        var markdown = $"""
            ## Análise operacional de triagens (PS)

            **Período:** últimos {days} dias · **Nível:** {risk}

            ### Indicadores
            {string.Join("\n", indicators.Select(i => $"- **{i.Label}:** {i.Value}"))}

            ### Recomendações operacionais
            {recommendations}
            """;

        var (finalMarkdown, groqEnriched) = await EnrichWithGroqAsync(
            """
            Você é gestor de pronto-socorro hospitalar. Analise indicadores AGREGADOS de fila e triagem.
            Responda em português, markdown, seções: Diagnóstico da fila, Gargalos prováveis, Ações imediatas (máx 5 bullets).
            Não invente números — use apenas os fornecidos.
            """,
            $"Atendimentos: {visits.Count}, aguardando: {waiting}, tempo médio espera (min): {avgWait}, nível: {risk}",
            markdown,
            cancellationToken);

        return await SaveInsightAsync(
            AiInsightType.TriageOperational,
            "Análise operacional de triagens",
            $"{served} atendimentos iniciados, {waiting} aguardando, espera média {avgWait} min.",
            risk,
            indicators,
            finalMarkdown,
            null,
            null,
            userId,
            groqEnriched,
            cancellationToken);
    }

    public async Task<PrescriptionSafetyResultDto> AnalyzePrescriptionSafetyAsync(
        Guid patientId,
        string prescriptionContent,
        CancellationToken cancellationToken = default)
    {
        var alerts = new List<ClinicalAlertDto>();
        var record = await dbContext.MedicalRecords.AsNoTracking()
            .FirstOrDefaultAsync(m => m.PatientId == patientId, cancellationToken);

        if (record is null)
        {
            return new PrescriptionSafetyResultDto(true, alerts, "Paciente sem prontuário — nenhuma checagem aplicável.");
        }

        var allergies = await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.MedicalRecordId == record.Id
                && (EF.Functions.ILike(e.Content, "%alerg%")
                    || EF.Functions.ILike(e.Content, "%Alergia%")))
            .Select(e => e.Content)
            .Take(10)
            .ToListAsync(cancellationToken);

        try
        {
            PrescriptionRules.ValidateNoAllergyConflict(prescriptionContent, allergies);
        }
        catch (InvalidOperationException ex)
        {
            alerts.Add(new ClinicalAlertDto(
                "ALLERGY",
                "critical",
                "Conflito com alergia",
                ex.Message,
                BusinessRuleCodes.PrescriptionAllergy));
        }

        var contentLower = prescriptionContent.ToLowerInvariant();
        var activePrescriptions = await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.MedicalRecordId == record.Id
                && e.EntryType == MedicalRecordEntryType.Prescription
                && e.IsActive
                && e.IsSigned
                && e.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            .Select(e => e.Content)
            .ToListAsync(cancellationToken);

        foreach (var existing in activePrescriptions)
        {
            var existingLower = existing.ToLowerInvariant();
            var duplicateTerms = contentLower.Split([' ', ',', ';', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 5 && existingLower.Contains(t, StringComparison.Ordinal))
                .Take(3)
                .ToList();

            if (duplicateTerms.Count >= 2)
            {
                alerts.Add(new ClinicalAlertDto(
                    "DUP-THERAPY",
                    "warning",
                    "Possível terapia duplicada",
                    $"Termos em comum com prescrição recente: {string.Join(", ", duplicateTerms)}.",
                    "RN-012"));
            }
        }

        if (alerts.Count == 0)
        {
            alerts.Add(new ClinicalAlertDto(
                "INTERACTION",
                "info",
                "Interações medicamentosas",
                "Verificação completa de bulário pendente — revisão manual recomendada.",
                "RN-012"));
        }

        var isSafe = !alerts.Any(a => a.Severity is "critical");
        var summary = isSafe
            ? "Nenhum bloqueio automático — revise alertas informativos."
            : "Prescrição com alertas críticos — revisão obrigatória antes de assinar.";

        return new PrescriptionSafetyResultDto(isSafe, alerts, summary);
    }

    public async Task<AiInsightReportDto> AnalyzeHospitalDashboardAsync(
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var totalBeds = await dbContext.Beds.CountAsync(b => b.IsActive, cancellationToken);
        var occupied = await dbContext.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Occupied, cancellationToken);
        var occupancy = totalBeds == 0 ? 0 : Math.Round((decimal)occupied / totalBeds * 100, 1);

        var psWaiting = await dbContext.EmergencyVisits.CountAsync(
            v => v.IsActive && v.Status == EmergencyVisitStatus.Waiting, cancellationToken);
        var lowStock = await dbContext.Products.CountAsync(
            p => p.IsActive && p.QuantityOnHand <= p.MinimumStock, cancellationToken);
        var expiringLots = await dbContext.ProductLots.CountAsync(
            l => l.IsActive && l.QuantityOnHand > 0
                && l.ExpiryDate != null
                && l.ExpiryDate <= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            cancellationToken);

        var risk = occupancy >= HospitalBusinessRules.CriticalBedOccupancyPercent
            || psWaiting >= 15
            || lowStock >= 10
                ? "Alerta"
                : occupancy >= 80 || psWaiting >= 8 || lowStock >= 5
                    ? "Atenção"
                    : "Normal";

        var indicators = new List<AiInsightIndicatorDto>
        {
            new("Ocupação de leitos (%)", occupancy.ToString(CultureInfo.InvariantCulture),
                occupancy >= 85 ? "danger" : null),
            new("Pacientes aguardando no PS", psWaiting.ToString(), psWaiting >= 10 ? "warning" : null),
            new("Produtos abaixo do mínimo", lowStock.ToString(), lowStock > 0 ? "warning" : null),
            new("Lotes a vencer (30d)", expiringLots.ToString(), expiringLots > 0 ? "warning" : null),
        };

        var markdown = $"""
            ## Painel hospitalar — visão assistencial

            **Nível operacional:** {risk}

            ### Indicadores
            {string.Join("\n", indicators.Select(i => $"- **{i.Label}:** {i.Value}"))}

            ### Condutas sugeridas
            {(risk == "Alerta"
                ? "- Acionar gestão de leitos e PS.\n- Priorizar reposição de estoque crítico.\n- Revisar lotes a vencer."
                : risk == "Atenção"
                    ? "- Monitorar ocupação e fila do PS nas próximas horas."
                    : "- Operação estável — manter vigilância rotineira.")}
            """;

        var (finalMarkdown, groqEnriched) = await EnrichWithGroqAsync(
            "Você é diretor clínico hospitalar. Resuma indicadores operacionais em português (markdown, máx 8 bullets).",
            $"Ocupação: {occupancy}%. PS aguardando: {psWaiting}. Estoque baixo: {lowStock}. Lotes vencendo: {expiringLots}.",
            markdown,
            cancellationToken);

        return await SaveInsightAsync(
            AiInsightType.HospitalDashboard,
            "Painel hospitalar — inteligência operacional",
            $"Ocupação {occupancy}%, {psWaiting} no PS, {lowStock} itens críticos de estoque.",
            risk,
            indicators,
            finalMarkdown,
            null,
            null,
            userId,
            groqEnriched,
            cancellationToken);
    }

    private async Task<string> EnrichSymptomsWithPatientHistoryAsync(
        TriageRequest request,
        CancellationToken cancellationToken)
    {
        var symptoms = request.Symptoms.Trim();
        if (!request.PatientId.HasValue)
        {
            return symptoms;
        }

        var patientId = request.PatientId.Value;
        var parts = new List<string> { symptoms };

        var allergies = await dbContext.MedicalRecordEntries.AsNoTracking()
            .Where(e => e.MedicalRecord.PatientId == patientId
                && (EF.Functions.ILike(e.Content, "%alerg%")
                    || EF.Functions.ILike(e.Content, "%Alergia%")))
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.Content)
            .Take(2)
            .ToListAsync(cancellationToken);

        foreach (var allergy in allergies)
        {
            var snippet = allergy.Length > 80 ? allergy[..80] : allergy;
            parts.Add($"Histórico alergia: {snippet}");
        }

        var priorVisits = await dbContext.EmergencyVisits.CountAsync(
            e => e.PatientId == patientId && e.IsActive && e.ArrivedAt >= DateTime.UtcNow.AddMonths(-6),
            cancellationToken);
        if (priorVisits > 0)
        {
            parts.Add($"Passagens PS (6m): {priorVisits}");
        }

        var lastComplaint = await dbContext.EmergencyVisits.AsNoTracking()
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.ArrivedAt)
            .Select(e => e.ChiefComplaint)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(lastComplaint))
        {
            parts.Add($"Última queixa PS: {lastComplaint}");
        }

        if (!string.IsNullOrWhiteSpace(request.HealthHistory))
        {
            parts.Add($"Histórico informado: {request.HealthHistory.Trim()}");
        }

        return string.Join(" | ", parts);
    }

    private async Task<(string Markdown, bool GroqEnriched)> EnrichWithGroqAsync(
        string systemPrompt,
        string userPrompt,
        string fallbackMarkdown,
        CancellationToken cancellationToken)
    {
        if (!groqLlm.IsConfigured)
        {
            return (fallbackMarkdown, false);
        }

        var groqText = await groqLlm.CompleteAsync(systemPrompt, userPrompt, cancellationToken);
        if (string.IsNullOrWhiteSpace(groqText))
        {
            return (fallbackMarkdown, false);
        }

        var combined = $"""
            {fallbackMarkdown}

            ---

            ### Análise Groq ({_groq.Model})
            {groqText}
            """;
        return (combined, true);
    }

    public async Task<IReadOnlyList<AiInsightReportDto>> GetInsightReportsAsync(
        int limit, AiInsightType? type, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var query = dbContext.IntegrationMessages.AsNoTracking()
            .Where(m => m.IsActive && m.Type == IntegrationMessageType.AiInsight);

        if (type.HasValue)
        {
            var typeStr = type.Value.ToString();
            query = query.Where(m => m.Source == typeStr);
        }

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return messages.Select(DeserializeInsight).Where(i => i is not null).Cast<AiInsightReportDto>().ToList();
    }

    public async Task<AiInsightReportDto?> GetInsightReportAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.IntegrationMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id && m.Type == IntegrationMessageType.AiInsight, cancellationToken);
        return message is null ? null : DeserializeInsight(message);
    }

    private async Task<AiInsightReportDto> SaveInsightAsync(
        AiInsightType type,
        string title,
        string summary,
        string riskLevel,
        IReadOnlyList<AiInsightIndicatorDto> indicators,
        string markdown,
        Guid? patientId,
        string? patientName,
        Guid? userId,
        bool groqEnriched,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var dto = new AiInsightReportDto(
            id, type, title, summary, riskLevel, indicators, markdown, DateTime.UtcNow,
            patientId, patientName, groqEnriched, groqEnriched ? _groq.Model : null);

        var message = new IntegrationMessage
        {
            Id = id,
            Type = IntegrationMessageType.AiInsight,
            Source = type.ToString(),
            Destination = userId?.ToString(),
            Payload = JsonSerializer.Serialize(new { type, patientId, days = type == AiInsightType.Outbreak ? 30 : 7 }),
            ResponsePayload = JsonSerializer.Serialize(dto),
            Status = IntegrationMessageStatus.Processed,
            PatientId = patientId,
        };

        dbContext.IntegrationMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        return dto;
    }

    private static AiInsightReportDto? DeserializeInsight(IntegrationMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.ResponsePayload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AiInsightReportDto>(message.ResponsePayload);
        }
        catch
        {
            return null;
        }
    }

    private static TriageUrgency ClassifyUrgency(string symptoms, TriageRequest request)
    {
        var scores = new List<TriageUrgency>
        {
            ClassifySymptoms(symptoms),
            ClassifyVitals(request),
            ClassifyPain(request.PainLevel)
        };

        return scores.Max();
    }

    private static TriageUrgency ClassifySymptoms(string text)
    {
        foreach (var rule in SymptomRules)
        {
            if (rule.Keywords.Any(k => text.Contains(k)))
            {
                return rule.Urgency;
            }
        }

        return TriageUrgency.Medium;
    }

    private static TriageUrgency ClassifyVitals(TriageRequest request)
    {
        var urgency = TriageUrgency.NonUrgent;

        if (request.OxygenSaturationPct is < 90)
        {
            return TriageUrgency.Emergency;
        }

        if (request.OxygenSaturationPct is >= 90 and <= 93)
        {
            urgency = Max(urgency, TriageUrgency.High);
        }

        if (request.HeartRateBpm is > 150 or < 40)
        {
            return TriageUrgency.Emergency;
        }

        if (request.HeartRateBpm is >= 120 and <= 150)
        {
            urgency = Max(urgency, TriageUrgency.High);
        }

        if (request.SystolicBp is < 80)
        {
            return TriageUrgency.Emergency;
        }

        if (request.SystolicBp is >= 80 and < 90)
        {
            urgency = Max(urgency, TriageUrgency.High);
        }

        if (request.SystolicBp is >= 180)
        {
            urgency = Max(urgency, TriageUrgency.High);
        }

        if (request.TemperatureC is >= 40)
        {
            urgency = Max(urgency, TriageUrgency.High);
        }
        else if (request.TemperatureC is >= 38.5m)
        {
            urgency = Max(urgency, TriageUrgency.Medium);
        }

        return urgency;
    }

    private static TriageUrgency ClassifyPain(int? painLevel)
    {
        if (painLevel is null)
        {
            return TriageUrgency.NonUrgent;
        }

        return painLevel switch
        {
            >= 9 => TriageUrgency.Emergency,
            >= 7 => TriageUrgency.High,
            >= 5 => TriageUrgency.Medium,
            >= 3 => TriageUrgency.Low,
            _ => TriageUrgency.NonUrgent
        };
    }

    private static TriageUrgency Max(TriageUrgency current, TriageUrgency candidate)
        => candidate > current ? candidate : current;

    private static string RecommendedSpecialty(TriageUrgency urgency, string symptoms)
    {
        if (symptoms.Contains("pediatr") || symptoms.Contains("criança") || symptoms.Contains("crianca"))
        {
            return "Pediatria";
        }

        return urgency switch
        {
            TriageUrgency.Emergency or TriageUrgency.High => "Emergência",
            TriageUrgency.NonUrgent => "Atenção Primária / UBS",
            _ => "Clínica Geral"
        };
    }

    private static string BuildAssessmentNotes(TriageRequest request)
    {
        var parts = new List<string>();

        if (request.SystolicBp.HasValue && request.DiastolicBp.HasValue)
        {
            parts.Add($"PA {request.SystolicBp}/{request.DiastolicBp} mmHg");
        }

        if (request.HeartRateBpm.HasValue)
        {
            parts.Add($"FC {request.HeartRateBpm} bpm");
        }

        if (request.TemperatureC.HasValue)
        {
            parts.Add($"Temp {request.TemperatureC:0.0}°C");
        }

        if (request.OxygenSaturationPct.HasValue)
        {
            parts.Add($"SpO2 {request.OxygenSaturationPct}%");
        }

        if (request.PainLevel.HasValue)
        {
            parts.Add($"Dor {request.PainLevel}/10");
        }

        if (!string.IsNullOrWhiteSpace(request.DocumentNumber))
        {
            parts.Add($"Doc {request.DocumentNumber.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(request.SusCardNumber))
        {
            parts.Add($"SUS {request.SusCardNumber.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(request.HealthInsuranceName))
        {
            parts.Add($"Convênio {request.HealthInsuranceName.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(request.HealthHistory))
        {
            var history = request.HealthHistory.Trim();
            if (history.Length > 120)
            {
                history = history[..120] + "...";
            }

            parts.Add($"Histórico: {history}");
        }

        return parts.Count > 0 ? string.Join(" | ", parts) : "Sem sinais vitais informados";
    }

    private async Task<AiTriageLog?> FindAvailableTriageForAdmissionAsync(
        Guid patientId,
        Guid? triageLogId,
        CancellationToken cancellationToken)
    {
        if (triageLogId.HasValue)
        {
            return await dbContext.AiTriageLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Id == triageLogId.Value
                        && t.PatientId == patientId
                        && t.IsActive,
                    cancellationToken);
        }

        var cutoff = DateTime.UtcNow.AddHours(-TriageAdmissionHelper.WindowHours);
        var usedTriageIds = dbContext.Hospitalizations
            .Where(h => h.AiTriageLogId != null)
            .Select(h => h.AiTriageLogId!.Value);

        return await dbContext.AiTriageLogs
            .AsNoTracking()
            .Where(t => t.PatientId == patientId
                && t.IsActive
                && t.CreatedAt >= cutoff
                && !usedTriageIds.Contains(t.Id))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static ManchesterInfo ManchesterMetadata(TriageUrgency urgency) => urgency switch
    {
        TriageUrgency.Emergency => new(
            "Emergência",
            "Vermelho",
            "#dc2626",
            0,
            "ImmediateConsultation",
            "Encaminhamento imediato ao consultório — risco imediato de morte.",
            "Classificação vermelha (Manchester): atendimento imediato. Monitorar continuamente até avaliação médica."),
        TriageUrgency.High => new(
            "Muito Urgente",
            "Laranja",
            "#ea580c",
            10,
            "ImmediateConsultation",
            "Encaminhamento prioritário — atendimento em até 10 minutos.",
            "Classificação laranja (Manchester): risco moderado a alto de agravamento. Avaliar em até 10 minutos."),
        TriageUrgency.Medium => new(
            "Urgente",
            "Amarelo",
            "#ca8a04",
            60,
            "WaitingRoom",
            "Sala de espera — atendimento em até 60 minutos.",
            "Classificação amarela (Manchester): gravidade moderada sem risco iminente. Reavaliar se piorar."),
        TriageUrgency.Low => new(
            "Pouco Urgente",
            "Verde",
            "#16a34a",
            120,
            "WaitingRoom",
            "Sala de espera — atendimento em até 120 minutos.",
            "Classificação verde (Manchester): baixa complexidade. Orientar sinais de alerta ao paciente."),
        TriageUrgency.NonUrgent => new(
            "Não Urgente",
            "Azul",
            "#2563eb",
            240,
            "UbsReferral",
            "Encaminhamento à UBS ou atendimento em até 240 minutos.",
            "Classificação azul (Manchester): caso simples. Considerar encaminhamento à Atenção Primária."),
        _ => new(
            "Urgente",
            "Amarelo",
            "#ca8a04",
            60,
            "WaitingRoom",
            "Sala de espera — atendimento em até 60 minutos.",
            "Classificação amarela (Manchester): gravidade moderada sem risco iminente.")
    };

    private async Task<IReadOnlyList<Cid10SuggestionDto>> SuggestCid10InternalAsync(
        string text, int maxResults, CancellationToken cancellationToken)
    {
        var normalizedText = text.ToLowerInvariant();
        var terms = normalizedText.Split([' ', ',', '.', ';'], StringSplitOptions.RemoveEmptyEntries);
        var catalog = await dbContext.Cid10Catalogs.AsNoTracking().Where(c => c.IsActive).ToListAsync(cancellationToken);

        var scored = catalog
            .Select(c =>
            {
                var codeLower = c.Code.ToLowerInvariant();
                var descLower = c.Description.ToLowerInvariant();
                var keywords = (c.Keywords ?? c.Description).ToLowerInvariant();
                var score = 0;

                foreach (var term in terms.Where(t => t.Length > 2))
                {
                    if (codeLower.Contains(term)) score += 2;
                    if (descLower.Contains(term)) score += 3;
                    if (keywords.Contains(term)) score += 4;
                }

                if (descLower.Contains(normalizedText)) score += 8;
                if (keywords.Contains(normalizedText)) score += 10;

                foreach (var phrase in ExtractPhrases(normalizedText))
                {
                    if (descLower.Contains(phrase) || keywords.Contains(phrase))
                    {
                        score += 6;
                    }
                }

                return new Cid10SuggestionDto(c.Code, c.Description, c.Category, score);
            })
            .Where(s => s.Score > 0)
            .OrderByDescending(s => s.Score)
            .Take(maxResults)
            .ToList();

        if (scored.Count > 0 || !groqLlm.IsConfigured)
        {
            return scored;
        }

        var groqText = await groqLlm.CompleteAsync(
            """
            Você é codificador CID-10. Dado o texto clínico, sugira até 3 códigos CID-10 no formato:
            CODIGO|Descrição (um por linha). Apenas códigos válidos brasileiros.
            """,
            $"Texto clínico: {text}",
            cancellationToken);

        if (string.IsNullOrWhiteSpace(groqText))
        {
            return scored;
        }

        var llmSuggestions = groqText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line =>
            {
                var parts = line.Split('|', 2);
                if (parts.Length < 2) return null;
                return new Cid10SuggestionDto(parts[0].Trim(), parts[1].Trim(), "LLM", 5);
            })
            .Where(s => s is not null)
            .Cast<Cid10SuggestionDto>()
            .Take(maxResults)
            .ToList();

        return llmSuggestions;
    }

    private static IEnumerable<string> ExtractPhrases(string text)
    {
        var markers = new[] { "dor ", "febre", "tosse", "dispneia", "náusea", "nausea", "cefaleia", "vômito", "vomito" };
        foreach (var marker in markers)
        {
            if (text.Contains(marker, StringComparison.Ordinal))
            {
                yield return marker.Trim();
            }
        }
    }

    private sealed record ManchesterInfo(
        string Label,
        string Color,
        string ColorHex,
        int MaxWaitMinutes,
        string Referral,
        string ReferralLabel,
        string Guidance);
}
