using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class TissClinicalSourceService(AppDbContext dbContext) : ITissClinicalSourceService
{
    public async Task<IReadOnlyList<TissClinicalSourceDto>> GetSourcesAsync(
        Guid? patientId,
        ClinicalDocumentKind? documentKind,
        TissGuideType? guideType,
        string? reportCode,
        bool pendingOnly,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.TissClinicalSources.AsNoTracking().Where(s => s.IsActive);

        if (patientId.HasValue)
            query = query.Where(s => s.PatientId == patientId.Value);
        if (documentKind.HasValue)
            query = query.Where(s => s.DocumentKind == documentKind.Value);
        if (guideType.HasValue)
            query = query.Where(s => s.GuideType == guideType.Value);
        if (!string.IsNullOrWhiteSpace(reportCode))
            query = query.Where(s => s.ReportCode == reportCode);

        if (pendingOnly)
        {
            query = query.Where(s => s.GeneratedAt == null
                && s.GeneratedTissGuideId == null
                && (s.GeneratedArtifactJson == null || s.GeneratedArtifactJson == ""));
        }

        return await query
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .Select(MapSource())
            .ToListAsync(cancellationToken);
    }

    public async Task<TissClinicalSourceDto?> GetSourceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.TissClinicalSources.AsNoTracking()
            .Where(s => s.Id == id && s.IsActive)
            .Select(MapSource())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TissClinicalSourceDto?> FindSourceAsync(
        ClinicalSourceLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.TissClinicalSources.AsNoTracking()
            .Where(s => s.IsActive
                && s.DocumentKind == request.DocumentKind
                && s.PatientId == request.PatientId
                && s.GuideType == request.GuideType);

        if (!string.IsNullOrWhiteSpace(request.ReportCode))
            query = query.Where(s => s.ReportCode == request.ReportCode);
        if (request.AppointmentId.HasValue)
            query = query.Where(s => s.AppointmentId == request.AppointmentId.Value);
        if (request.HospitalizationId.HasValue)
            query = query.Where(s => s.HospitalizationId == request.HospitalizationId.Value);
        if (request.ChemotherapySessionId.HasValue)
            query = query.Where(s => s.ChemotherapySessionId == request.ChemotherapySessionId.Value);
        if (request.SurgeryId.HasValue)
            query = query.Where(s => s.SurgeryId == request.SurgeryId.Value);
        if (request.LabOrderId.HasValue)
            query = query.Where(s => s.LabOrderId == request.LabOrderId.Value);
        if (request.ImagingStudyId.HasValue)
            query = query.Where(s => s.ImagingStudyId == request.ImagingStudyId.Value);

        return await query
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .Select(MapSource())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TissClinicalSourceDto> UpsertSourceAsync(
        UpsertTissClinicalSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FormDataJson))
            throw new InvalidOperationException("Informe os dados do documento.");

        if (!await dbContext.Patients.AsNoTracking()
                .AnyAsync(p => p.Id == request.PatientId && p.IsActive, cancellationToken))
            throw new InvalidOperationException("Paciente não encontrado.");

        if (request.HealthInsuranceId.HasValue
            && !await dbContext.HealthInsurances.AsNoTracking()
                .AnyAsync(h => h.Id == request.HealthInsuranceId.Value && h.IsActive, cancellationToken))
            throw new InvalidOperationException("Convênio não encontrado.");

        if (request.DocumentKind == ClinicalDocumentKind.Report && string.IsNullOrWhiteSpace(request.ReportCode))
            throw new InvalidOperationException("Informe o código do relatório.");

        var existing = await dbContext.TissClinicalSources
            .FirstOrDefaultAsync(s => s.IsActive
                && s.DocumentKind == request.DocumentKind
                && s.PatientId == request.PatientId
                && s.GuideType == request.GuideType
                && s.ReportCode == request.ReportCode
                && s.AppointmentId == request.AppointmentId
                && s.HospitalizationId == request.HospitalizationId
                && s.ChemotherapySessionId == request.ChemotherapySessionId
                && s.SurgeryId == request.SurgeryId
                && s.LabOrderId == request.LabOrderId
                && s.ImagingStudyId == request.ImagingStudyId, cancellationToken);

        if (existing is null)
        {
            existing = new TissClinicalSource
            {
                PatientId = request.PatientId,
                DocumentKind = request.DocumentKind,
                GuideType = request.GuideType,
            };
            dbContext.TissClinicalSources.Add(existing);
        }

        existing.HealthInsuranceId = request.HealthInsuranceId;
        existing.ReportCode = request.ReportCode?.Trim();
        existing.AppointmentId = request.AppointmentId;
        existing.HospitalizationId = request.HospitalizationId;
        existing.ChemotherapySessionId = request.ChemotherapySessionId;
        existing.SurgeryId = request.SurgeryId;
        existing.LabOrderId = request.LabOrderId;
        existing.ImagingStudyId = request.ImagingStudyId;
        existing.Label = request.Label.Trim();
        existing.FormDataJson = request.FormDataJson.Trim();
        existing.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetSourceByIdAsync(existing.Id, cancellationToken))!;
    }

    public async Task<TissClinicalSourceDto?> LinkGeneratedGuideAsync(
        Guid sourceId,
        LinkClinicalSourceGuideRequest request,
        CancellationToken cancellationToken = default)
    {
        var source = await dbContext.TissClinicalSources
            .FirstOrDefaultAsync(s => s.Id == sourceId && s.IsActive, cancellationToken);
        if (source is null)
            return null;

        var guideExists = await dbContext.TissGuides.AsNoTracking()
            .AnyAsync(g => g.Id == request.TissGuideId && g.IsActive, cancellationToken);
        if (!guideExists)
            throw new InvalidOperationException("Guia TISS não encontrada.");

        source.GeneratedTissGuideId = request.TissGuideId;
        source.GeneratedAt = DateTime.UtcNow;
        source.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetSourceByIdAsync(sourceId, cancellationToken);
    }

    public async Task<TissClinicalSourceDto?> LinkGeneratedArtifactAsync(
        Guid sourceId,
        LinkClinicalSourceArtifactRequest request,
        CancellationToken cancellationToken = default)
    {
        var source = await dbContext.TissClinicalSources
            .FirstOrDefaultAsync(s => s.Id == sourceId && s.IsActive, cancellationToken);
        if (source is null)
            return null;

        if (string.IsNullOrWhiteSpace(request.ArtifactJson))
            throw new InvalidOperationException("Informe o resultado gerado.");

        source.GeneratedArtifactJson = request.ArtifactJson.Trim();
        source.GeneratedAt = DateTime.UtcNow;
        source.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetSourceByIdAsync(sourceId, cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<TissClinicalSource, TissClinicalSourceDto>> MapSource() =>
        s => new TissClinicalSourceDto(
            s.Id,
            s.DocumentKind,
            s.PatientId,
            s.Patient.FullName,
            s.HealthInsuranceId,
            s.HealthInsurance != null ? s.HealthInsurance.Name : null,
            s.GuideType,
            s.ReportCode,
            s.AppointmentId,
            s.HospitalizationId,
            s.ChemotherapySessionId,
            s.SurgeryId,
            s.LabOrderId,
            s.ImagingStudyId,
            s.Label,
            s.FormDataJson,
            s.GeneratedTissGuideId,
            s.GeneratedTissGuide != null ? s.GeneratedTissGuide.GuideNumber : null,
            s.GeneratedArtifactJson,
            s.GeneratedAt,
            s.CreatedAt,
            s.UpdatedAt);
}
