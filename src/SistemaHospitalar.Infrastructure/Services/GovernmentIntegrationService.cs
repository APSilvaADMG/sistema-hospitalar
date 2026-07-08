using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Government;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Government;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class GovernmentIntegrationService(AppDbContext dbContext) : IGovernmentIntegrationService
{
    private const string DefaultCnes = "2277185";
    private const string BpaProcedureCode = "0301010072";
    private const string ChemoProcedureCode = "0304030188";
    private const string DialysisProcedureCode = "0304050079";    public IReadOnlyList<GovIntegrationProfileDto> GetProfiles() => GovernmentIntegrationProfiles.All;

    public async Task<CnsLookupResultDto> LookupCnsAsync(string cns, CancellationToken cancellationToken = default)
    {
        var mock = BuildCnsLookup(cns);
        if (!mock.Found)
        {
            return mock;
        }

        dbContext.IntegrationMessages.Add(new IntegrationMessage
        {
            Type = IntegrationMessageType.CnsLookup,
            Source = "CADSUS/CNS",
            Destination = "SistemaHospitalar",
            Payload = JsonSerializer.Serialize(new { cns = mock.Cns }),
            Status = IntegrationMessageStatus.Processed,
            ResponsePayload = mock.FullName
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return mock;
    }

    public async Task<CnesEstablishmentDto> LookupCnesEstablishmentAsync(
        string cnesCode, CancellationToken cancellationToken = default)
    {
        var code = cnesCode.Trim().PadLeft(7, '0')[..7];

        var result = new CnesEstablishmentDto(
            code,
            "Hospital APSMedCore Demonstração",
            "APSMedCore Hospital",
            "Av. Paulista, 1000",
            "São Paulo",
            "SP",
            "Municipal",
            [
                new CnesProfessionalDto("Dra. Ana Paula Silva", "898001234567890", "225125", "Clínica Geral", "Médico"),
                new CnesProfessionalDto("Enf. Roberto Lima", "898009876543210", "223505", "Enfermagem", "Enfermeiro"),
            ]);

        dbContext.IntegrationMessages.Add(new IntegrationMessage
        {
            Type = IntegrationMessageType.CnesLookup,
            Source = "CNES/DATASUS",
            Destination = "SistemaHospitalar",
            Payload = JsonSerializer.Serialize(new { cnes = code }),
            Status = IntegrationMessageStatus.Processed,
            ResponsePayload = result.Name
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<GovIntegrationActionResultDto> ApplyCnsToPatientAsync(
        Guid patientId, ApplyCnsToPatientRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.Patients
            .Include(p => p.Insurances.Where(i => i.IsActive))
            .FirstOrDefaultAsync(p => p.Id == patientId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Paciente não encontrado.");

        var cns = NormalizeCns(request.Cns);
        var lookup = BuildCnsLookup(request.Cns);

        if (!lookup.Found)
        {
            return new GovIntegrationActionResultDto(null, false, lookup.Message ?? "CNS não encontrado.", null);
        }

        patient.Cns = cns;
        if (lookup.FullName is not null && string.IsNullOrWhiteSpace(patient.FullName))
        {
            patient.FullName = lookup.FullName;
        }

        if (lookup.BirthDate.HasValue && patient.BirthDate == default)
        {
            patient.BirthDate = lookup.BirthDate.Value;
        }

        if (!string.IsNullOrWhiteSpace(lookup.MotherName))
        {
            patient.MotherName = lookup.MotherName;
        }

        if (!string.IsNullOrWhiteSpace(lookup.AddressCity))
        {
            patient.AddressCity = lookup.AddressCity;
        }

        if (!string.IsNullOrWhiteSpace(lookup.AddressState))
        {
            patient.AddressState = lookup.AddressState;
        }

        var susInsurance = patient.Insurances
            .FirstOrDefault(i => i.HealthInsurance.Name.Contains("SUS", StringComparison.OrdinalIgnoreCase));

        if (susInsurance is not null)
        {
            susInsurance.CnsNumber = cns;
            susInsurance.UpdatedAt = DateTime.UtcNow;
        }

        patient.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new GovIntegrationActionResultDto(null, true, "CNS aplicado ao cadastro do paciente.", cns);
    }

    public async Task<SihAihPreviewDto?> GenerateSihAihPreviewAsync(
        Guid hospitalizationId, CancellationToken cancellationToken = default)
    {
        var hosp = await dbContext.Hospitalizations
            .Include(h => h.Patient).ThenInclude(p => p.Insurances)
            .Include(h => h.Bed).ThenInclude(b => b.Ward)
            .FirstOrDefaultAsync(h => h.Id == hospitalizationId && h.IsActive, cancellationToken);

        if (hosp is null)
        {
            return null;
        }

        HospitalBusinessRules.ValidateBillingAccountClosed(hosp.BillingAccountClosedAt);

        var los = Math.Max(1, (int)Math.Ceiling((DateTime.UtcNow - hosp.AdmittedAt).TotalDays));
        var competence = hosp.SusCompetence ?? DateTime.UtcNow.ToString("yyyyMM");
        var aihNumber = hosp.AihNumber
            ?? $"AIH{competence}{hosp.Id.ToString("N")[..6].ToUpperInvariant()}";
        var procedure = hosp.PrimarySigtapProcedureCode ?? "—";
        var cid = hosp.PrimaryCid10Code ?? hosp.Diagnosis;

        var preview = new SihAihPreviewDto(
            hosp.Id,
            hosp.Patient.FullName,
            hosp.Patient.Cns ?? hosp.Patient.Insurances.FirstOrDefault()?.CnsNumber,
            hosp.Bed.Ward.Name,
            hosp.Bed.BedNumber,
            hosp.AdmittedAt,
            los,
            hosp.Diagnosis,
            hosp.PrimaryCid10Code,
            hosp.SecondaryCid10Code,
            hosp.PrimarySigtapProcedureCode,
            hosp.SecondarySigtapProcedureCode,
            hosp.SusCharacter?.ToString(),
            hosp.SusModality?.ToString(),
            hosp.CnesCode,
            hosp.SusAuthorizationNumber,
            competence,
            aihNumber,
            $"AIH — CID {cid ?? "—"} · proc. {procedure} · {los} dia(s) · caráter {hosp.SusCharacter?.ToString() ?? "—"}.");

        hosp.AihExportedAt = DateTime.UtcNow;
        hosp.AihNumber ??= aihNumber;
        hosp.SusCompetence ??= competence;
        hosp.UpdatedAt = DateTime.UtcNow;

        dbContext.IntegrationMessages.Add(new IntegrationMessage
        {
            Type = IntegrationMessageType.SihExport,
            Source = "SistemaHospitalar",
            Destination = "SIH-SUS",
            Payload = JsonSerializer.Serialize(preview),
            Status = IntegrationMessageStatus.Processed,
            PatientId = hosp.PatientId
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return preview;
    }

    public async Task<SiaDocumentPreviewDto> GenerateSiaPreviewAsync(
        SiaDocumentType documentType, string competence, CancellationToken cancellationToken = default)
    {
        var (comp, start, end) = ParseCompetence(competence);
        var lines = documentType == SiaDocumentType.Apac
            ? await LoadApacLinesAsync(start, end, cancellationToken)
            : await LoadBpaLinesAsync(start, end, cancellationToken);

        var estimated = lines.Sum(l => l.UnitValue * l.Quantity);
        var summary = documentType == SiaDocumentType.Bpa
            ? $"BPA consolidado — {lines.Count} procedimento(s) ambulatorial(is) na competência {comp}."
            : $"APAC — {lines.Count} procedimento(s) de alta complexidade (oncologia/diálise) na competência {comp}.";

        var preview = new SiaDocumentPreviewDto(
            documentType,
            comp,
            lines.Count,
            estimated,
            summary,
            lines);

        dbContext.IntegrationMessages.Add(new IntegrationMessage
        {
            Type = IntegrationMessageType.SiaExport,
            Source = "SistemaHospitalar",
            Destination = "SIA-SUS",
            Payload = JsonSerializer.Serialize(preview),
            Status = IntegrationMessageStatus.Processed
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return preview;
    }

    public async Task<DatasusExportFileDto> ExportSiaDocumentAsync(
        SiaDocumentType documentType, string competence, CancellationToken cancellationToken = default)
    {
        var (comp, start, end) = ParseCompetence(competence);
        var lines = documentType == SiaDocumentType.Apac
            ? await LoadApacLinesAsync(start, end, cancellationToken)
            : await LoadBpaLinesAsync(start, end, cancellationToken);

        var label = documentType == SiaDocumentType.Bpa ? "BPA" : "APAC";
        var prefix = documentType == SiaDocumentType.Bpa ? "PA" : "APAC";

        var exportLines = lines
            .Select(l => new DatasusExportBuilder.ExportLine(
                "02",
                $"{DatasusExportBuilder.PadNumeric(l.PatientCns, 15)}|{l.ProcedureCode}|{l.ServiceDate:yyyyMMdd}|{l.Quantity}|{DatasusExportBuilder.PadField(l.ProfessionalCbo ?? "225125", 6)}|{DatasusExportBuilder.PadField(l.PatientName, 60)}|{l.UnitValue:F2}"))
            .ToList();

        var doc = DatasusExportBuilder.Build(prefix, comp, DefaultCnes, label, exportLines);

        dbContext.IntegrationMessages.Add(new IntegrationMessage
        {
            Type = IntegrationMessageType.SiaExport,
            Source = "SistemaHospitalar",
            Destination = "SIA-SUS-ARQUIVO",
            Payload = JsonSerializer.Serialize(new { comp, label, doc.RecordCount, doc.ChecksumSha256 }),
            Status = IntegrationMessageStatus.Processed,
            ResponsePayload = doc.FileName
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToExportDto(doc, comp, label);
    }

    public async Task<DatasusExportFileDto> ExportSihAihBatchAsync(
        string competence, CancellationToken cancellationToken = default)
    {
        var (comp, start, end) = ParseCompetence(competence);

        var hospitalizations = await dbContext.Hospitalizations.AsNoTracking()
            .Include(h => h.Patient)
            .Include(h => h.Bed).ThenInclude(b => b.Ward)
            .Where(h => h.IsActive && h.BillingAccountClosedAt != null
                && h.AdmittedAt >= start && h.AdmittedAt < end)
            .ToListAsync(cancellationToken);

        var exportLines = hospitalizations.Select(h =>
        {
            var los = Math.Max(1, (int)Math.Ceiling(((h.DischargedAt ?? DateTime.UtcNow) - h.AdmittedAt).TotalDays));
            var aih = h.AihNumber ?? $"AIH{comp}{h.Id.ToString("N")[..6].ToUpperInvariant()}";
            return new DatasusExportBuilder.ExportLine(
                "02",
                $"{DatasusExportBuilder.PadField(aih, 13)}|{DatasusExportBuilder.PadNumeric(h.Patient.Cns, 15)}|{DatasusExportBuilder.PadField(h.PrimaryCid10Code ?? h.Diagnosis ?? "Z000", 6)}|{DatasusExportBuilder.PadField(h.PrimarySigtapProcedureCode ?? "0303140119", 10)}|{h.AdmittedAt:yyyyMMdd}|{los}|{DatasusExportBuilder.PadField(h.Patient.FullName, 60)}|{DatasusExportBuilder.PadField(h.CnesCode ?? DefaultCnes, 7)}");
        }).ToList();

        var doc = DatasusExportBuilder.Build("RD", comp, DefaultCnes, "AIH", exportLines);

        dbContext.IntegrationMessages.Add(new IntegrationMessage
        {
            Type = IntegrationMessageType.SihExport,
            Source = "SistemaHospitalar",
            Destination = "SIH-SUS-ARQUIVO",
            Payload = JsonSerializer.Serialize(new { comp, doc.RecordCount, doc.ChecksumSha256 }),
            Status = IntegrationMessageStatus.Processed,
            ResponsePayload = doc.FileName
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToExportDto(doc, comp, "AIH");
    }

    public async Task<DatasusExportFileDto> ExportCihaDocumentAsync(
        string competence, CancellationToken cancellationToken = default)
    {
        var (comp, start, end) = ParseCompetence(competence);
        var lines = await LoadApacLinesAsync(start, end, cancellationToken);

        var exportLines = lines.Select(l => new DatasusExportBuilder.ExportLine(
            "02",
            $"{DatasusExportBuilder.PadNumeric(l.PatientCns, 15)}|{l.ProcedureCode}|{l.ProcedureLabel}|{l.ServiceDate:yyyyMMdd}|{l.Quantity}|CIHA|{DatasusExportBuilder.PadField(l.PatientName, 60)}"))
            .ToList();

        var doc = DatasusExportBuilder.Build("CIHA", comp, DefaultCnes, "CIHA", exportLines);

        dbContext.IntegrationMessages.Add(new IntegrationMessage
        {
            Type = IntegrationMessageType.SiaExport,
            Source = "SistemaHospitalar",
            Destination = "CIHA-SUS",
            Payload = JsonSerializer.Serialize(new { comp, doc.RecordCount, doc.ChecksumSha256 }),
            Status = IntegrationMessageStatus.Processed,
            ResponsePayload = doc.FileName
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToExportDto(doc, comp, "CIHA");
    }

    private async Task<IReadOnlyList<SiaProductionLineDto>> LoadBpaLinesAsync(
        DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        var appointments = await dbContext.Appointments.AsNoTracking()
            .Where(a => a.IsActive && a.Status == AppointmentStatus.Completed
                && a.ScheduledAt >= start && a.ScheduledAt < end)
            .Select(a => new SiaProductionLineDto(
                a.Patient.FullName,
                a.Patient.Cns,
                BpaProcedureCode,
                "Consulta médica ambulatorial",
                a.ScheduledAt,
                1,
                45.50m,
                a.Professional.Crm != null ? "225125" : null))
            .Take(2000)
            .ToListAsync(cancellationToken);

        return appointments;
    }

    private async Task<IReadOnlyList<SiaProductionLineDto>> LoadApacLinesAsync(
        DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        var chemo = await dbContext.ChemotherapySessions.AsNoTracking()
            .Where(c => c.IsActive && c.ScheduledAt >= start && c.ScheduledAt < end
                && c.Status != ChemotherapySessionStatus.Cancelled)
            .Select(c => new SiaProductionLineDto(
                c.Patient.FullName,
                c.Patient.Cns,
                ChemoProcedureCode,
                $"Quimioterapia — {c.ProtocolName}",
                c.ScheduledAt,
                1,
                850m,
                "225125"))
            .ToListAsync(cancellationToken);

        var dialysis = await dbContext.DialysisSessions.AsNoTracking()
            .Where(d => d.IsActive && d.ScheduledAt >= start && d.ScheduledAt < end
                && d.Status != DialysisSessionStatus.Cancelled)
            .Select(d => new SiaProductionLineDto(
                d.Patient.FullName,
                d.Patient.Cns,
                DialysisProcedureCode,
                "Sessão de hemodiálise",
                d.ScheduledAt,
                1,
                620m,
                "223505"))
            .ToListAsync(cancellationToken);

        return chemo.Concat(dialysis).OrderBy(l => l.ServiceDate).ToList();
    }

    private static (string Competence, DateTime Start, DateTime End) ParseCompetence(string competence)
    {
        var comp = string.IsNullOrWhiteSpace(competence)
            ? DateTime.UtcNow.ToString("yyyyMM")
            : competence.Trim();

        if (comp.Length < 6 || !int.TryParse(comp[..4], out var year) || !int.TryParse(comp[4..6], out var month))
        {
            var now = DateTime.UtcNow;
            comp = now.ToString("yyyyMM");
            year = now.Year;
            month = now.Month;
        }

        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (comp, start, start.AddMonths(1));
    }

    private static DatasusExportFileDto ToExportDto(
        DatasusExportBuilder.ExportDocument doc, string competence, string documentType) => new(
        doc.FileName,
        "text/plain; charset=utf-8",
        doc.Content,
        doc.RecordCount,
        doc.ChecksumSha256,
        DatasusExportBuilder.LayoutVersion,
        competence,
        documentType);

    public async Task<RndsPatientSummaryDto?> QueryRndsPatientAsync(
        Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId && p.IsActive, cancellationToken);

        if (patient is null)
        {
            return null;
        }

        var items = new List<RndsClinicalItemDto>();

        var vaccinations = await dbContext.PatientVaccinations
            .AsNoTracking()
            .Where(v => v.PatientId == patientId && v.IsActive)
            .OrderByDescending(v => v.AdministeredAt)
            .Take(5)
            .Select(v => new { v.VaccineCatalog.Name, v.AdministeredAt, v.DoseNumber })
            .ToListAsync(cancellationToken);

        if (vaccinations.Count > 0)
        {
            items.AddRange(vaccinations.Select(v =>
                new RndsClinicalItemDto(
                    "Vacinação",
                    $"{v.Name} — dose {v.DoseNumber}",
                    v.AdministeredAt,
                    "APSMedCore")));
        }
        else
        {
            items.Add(new RndsClinicalItemDto("Vacinação", "Sem registros locais", DateTime.UtcNow, "APSMedCore"));
        }

        items.Add(new RndsClinicalItemDto("Exame", "Hemograma completo", DateTime.UtcNow.AddMonths(-2), "RNDS mock"));

        var labOrders = await dbContext.LabOrders
            .AsNoTracking()
            .Where(o => o.PatientId == patientId && o.IsActive)
            .OrderByDescending(o => o.CreatedAt)
            .Take(3)
            .ToListAsync(cancellationToken);

        items.AddRange(labOrders.Select(o =>
            new RndsClinicalItemDto("Exame local", $"Pedido laboratorial", o.CreatedAt, "APSMedCore")));

        dbContext.IntegrationMessages.Add(new IntegrationMessage
        {
            Type = IntegrationMessageType.RndsQuery,
            Source = "RNDS",
            Destination = "SistemaHospitalar",
            Payload = JsonSerializer.Serialize(new { patientId }),
            Status = IntegrationMessageStatus.Processed,
            PatientId = patientId,
            ResponsePayload = $"{items.Count} registro(s)"
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RndsPatientSummaryDto(patientId, patient.FullName, true, items);
    }

    private static string NormalizeCns(string cns)
        => new string(cns.Where(char.IsDigit).ToArray());

    private static CnsLookupResultDto BuildCnsLookup(string cns)
    {
        var normalized = NormalizeCns(cns);
        if (normalized.Length != PatientCnsRules.RequiredLength)
        {
            return new CnsLookupResultDto(false, normalized, null, null, null, null, null, null, "CNS inválido — informe 15 dígitos.");
        }

        if (!PatientCnsRules.IsValidChecksum(normalized))
        {
            return new CnsLookupResultDto(false, normalized, null, null, null, null, null, null, "CNS inválido — dígito verificador incorreto.");
        }

        return new CnsLookupResultDto(
            true,
            normalized,
            "Maria da Silva Santos",
            new DateOnly(1985, 3, 12),
            "Ana da Silva Santos",
            "Feminino",
            "São Paulo",
            "SP",
            "Dados simulados CADSUS — substituir por API oficial após credenciamento.");
    }
}
