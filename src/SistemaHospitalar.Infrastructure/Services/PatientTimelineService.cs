using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Patients;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class PatientTimelineService(
    AppDbContext db,
    IClinicalIntelligenceService clinicalIntelligence) : IPatientTimelineService
{
    private const int MaxEvents = 50;

    public async Task<PatientTimelineDto?> GetTimelineAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId && p.IsActive, cancellationToken);
        if (patient is null) return null;

        var events = new List<PatientTimelineEventDto>();

        var appointments = await db.Appointments.AsNoTracking()
            .Include(a => a.Professional)
            .Where(a => a.PatientId == patientId && a.IsActive)
            .OrderByDescending(a => a.ScheduledAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var appt in appointments)
        {
            events.Add(new PatientTimelineEventDto(
                "appointment",
                "Consulta agendada",
                $"Status: {appt.Status} — {appt.Notes ?? "Sem observações"}",
                appt.ScheduledAt,
                appt.Professional?.FullName,
                $"/recepcao/agendamentos?patientId={patientId}"));
        }

        var triages = await db.AiTriageLogs.AsNoTracking()
            .Where(t => t.PatientId == patientId && t.IsActive)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var triage in triages)
        {
            events.Add(new PatientTimelineEventDto(
                "triage",
                "Triagem de risco",
                $"{triage.Urgency}: {triage.Symptoms}",
                triage.CreatedAt,
                null,
                "/emergencia/classificacao-risco"));
        }

        var hospitalizations = await db.Hospitalizations.AsNoTracking()
            .Include(h => h.Professional)
            .Include(h => h.Bed).ThenInclude(b => b.Ward)
            .Where(h => h.PatientId == patientId && h.IsActive)
            .OrderByDescending(h => h.AdmittedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var hosp in hospitalizations)
        {
            var ward = hosp.Bed?.Ward?.Name ?? "—";
            var bed = hosp.Bed?.BedNumber ?? "—";
            events.Add(new PatientTimelineEventDto(
                "hospitalization",
                hosp.Status == HospitalizationStatus.Active ? "Internação ativa" : "Internação",
                $"{ward} · Leito {bed} — {hosp.Reason}",
                hosp.AdmittedAt,
                hosp.Professional?.FullName,
                $"/internacao/leitos?patientId={patientId}"));

            if (hosp.DischargedAt.HasValue)
            {
                events.Add(new PatientTimelineEventDto(
                    "discharge",
                    "Alta hospitalar",
                    $"{ward} · Leito {bed}",
                    hosp.DischargedAt.Value,
                    hosp.Professional?.FullName,
                    $"/internacao/leitos?patientId={patientId}"));
            }
        }

        var hospitalizationIds = hospitalizations.Select(h => h.Id).ToList();
        if (hospitalizationIds.Count > 0)
        {
            var transfers = await db.BedTransfers.AsNoTracking()
                .Include(t => t.FromBed).ThenInclude(b => b.Ward)
                .Include(t => t.ToBed).ThenInclude(b => b.Ward)
                .Include(t => t.Professional)
                .Where(t => hospitalizationIds.Contains(t.HospitalizationId) && t.IsActive)
                .OrderByDescending(t => t.TransferredAt)
                .Take(10)
                .ToListAsync(cancellationToken);

            foreach (var transfer in transfers)
            {
                events.Add(new PatientTimelineEventDto(
                    "bed_transfer",
                    "Transferência de leito",
                    $"{transfer.FromBed.Ward.Name} {transfer.FromBed.BedNumber} → {transfer.ToBed.Ward.Name} {transfer.ToBed.BedNumber}",
                    transfer.TransferredAt,
                    transfer.Professional?.FullName,
                    $"/internacao/leitos?patientId={patientId}"));
            }

            var eventLogs = await db.HospitalEventLogs.AsNoTracking()
                .Where(e => e.IsActive
                    && e.RelatedEntityId != null
                    && hospitalizationIds.Contains(e.RelatedEntityId.Value))
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .ToListAsync(cancellationToken);

            foreach (var log in eventLogs)
            {
                events.Add(new PatientTimelineEventDto(
                    "system_event",
                    log.EventType,
                    log.Status.ToString(),
                    log.ProcessedAt ?? log.CreatedAt,
                    null,
                    "/dashboard/command-center"));
            }
        }

        var emergencyVisits = await db.EmergencyVisits.AsNoTracking()
            .Where(v => v.PatientId == patientId && v.IsActive)
            .OrderByDescending(v => v.ArrivedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var visit in emergencyVisits)
        {
            events.Add(new PatientTimelineEventDto(
                "emergency",
                visit.Status == EmergencyVisitStatus.Waiting ? "Pronto-socorro — aguardando" : "Pronto-socorro",
                $"{visit.Urgency}: {visit.ChiefComplaint}",
                visit.ArrivedAt,
                null,
                "/emergencia"));
        }

        var entries = await db.MedicalRecordEntries.AsNoTracking()
            .Include(e => e.Professional)
            .Include(e => e.MedicalRecord)
            .Where(e => e.MedicalRecord.PatientId == patientId && e.IsActive)
            .OrderByDescending(e => e.CreatedAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        foreach (var entry in entries)
        {
            var type = entry.EntryType switch
            {
                MedicalRecordEntryType.Prescription => "prescription",
                MedicalRecordEntryType.ExamRequest => "exam_request",
                _ => "clinical_note",
            };
            var title = entry.EntryType switch
            {
                MedicalRecordEntryType.Prescription => entry.IsSigned ? "Prescrição assinada" : "Prescrição pendente de assinatura",
                MedicalRecordEntryType.ExamRequest => "Solicitação de exame",
                MedicalRecordEntryType.Anamnesis => "Anamnese",
                MedicalRecordEntryType.Evolution => "Evolução clínica",
                MedicalRecordEntryType.Procedure => "Procedimento",
                _ => "Registro clínico",
            };
            var snippet = entry.Content.Length > 120 ? entry.Content[..120] + "…" : entry.Content;
            events.Add(new PatientTimelineEventDto(
                type,
                title,
                snippet,
                entry.SignedAt ?? entry.CreatedAt,
                entry.Professional?.FullName,
                $"/pep?patientId={patientId}"));
        }

        var labOrders = await db.LabOrders.AsNoTracking()
            .Include(o => o.RequestingProfessional)
            .Include(o => o.Items).ThenInclude(i => i.LabExamCatalog)
            .Where(o => o.PatientId == patientId && o.IsActive)
            .OrderByDescending(o => o.CreatedAt)
            .Take(15)
            .ToListAsync(cancellationToken);

        foreach (var order in labOrders)
        {
            var exams = string.Join(", ", order.Items.Select(i => i.LabExamCatalog?.Name ?? "Exame"));
            events.Add(new PatientTimelineEventDto(
                "lab_order",
                "Pedido laboratorial",
                $"{order.Status}: {exams}",
                order.CreatedAt,
                order.RequestingProfessional?.FullName,
                $"/laboratorio?patientId={patientId}"));
        }

        var stockIssues = await db.StockIssues.AsNoTracking()
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Where(s => s.PatientId == patientId && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var issue in stockIssues)
        {
            var items = string.Join(", ", issue.Items.Select(i => $"{i.Product.Name} ({i.Quantity})"));
            events.Add(new PatientTimelineEventDto(
                "stock_issue",
                "Saída de material",
                $"{issue.SectorName}: {items}",
                issue.CreatedAt,
                issue.ResponsibleName,
                "/estoque/saida"));
        }

        var vitals = await db.VitalSignRecords.AsNoTracking()
            .Include(v => v.RecordedByProfessional)
            .Include(v => v.Hospitalization)
            .Where(v => v.Hospitalization.PatientId == patientId && v.IsActive)
            .OrderByDescending(v => v.RecordedAt)
            .Take(15)
            .ToListAsync(cancellationToken);

        foreach (var vital in vitals)
        {
            events.Add(new PatientTimelineEventDto(
                "vital_signs",
                "Sinais vitais",
                $"PA {vital.SystolicBp}/{vital.DiastolicBp} · FC {vital.HeartRate} · SpO₂ {vital.SpO2}%",
                vital.RecordedAt,
                vital.RecordedByProfessional?.FullName,
                $"/enfermagem/sinais-vitais?patientId={patientId}"));
        }

        var alerts = await clinicalIntelligence.GetPatientClinicalAlertsAsync(patientId, cancellationToken);
        if (alerts is not null)
        {
            foreach (var alert in alerts.Alerts)
            {
                events.Add(new PatientTimelineEventDto(
                    "clinical_alert",
                    alert.Title,
                    alert.Message,
                    DateTime.UtcNow,
                    null,
                    $"/recepcao/pacientes/listar?patientId={patientId}"));
            }
        }

        var sorted = events
            .OrderByDescending(e => e.At)
            .Take(MaxEvents)
            .ToList();

        return new PatientTimelineDto(patientId, patient.FullName, sorted);
    }
}
