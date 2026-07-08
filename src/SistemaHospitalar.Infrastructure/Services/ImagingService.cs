using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Imaging;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class ImagingService(AppDbContext dbContext) : IImagingService
{
    public async Task<IReadOnlyList<ImagingStudyDto>> GetStudiesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ImagingStudies
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.ScheduledAt)
            .Select(MapStudy())
            .ToListAsync(cancellationToken);
    }

    public async Task<ImagingStudyDto> CreateStudyAsync(
        CreateImagingStudyRequest request, CancellationToken cancellationToken = default)
    {
        var scheduledAt = request.ScheduledAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Utc)
            : request.ScheduledAt.ToUniversalTime();

        var count = await dbContext.ImagingStudies.CountAsync(cancellationToken);

        var study = new ImagingStudy
        {
            PatientId = request.PatientId,
            RequestingProfessionalId = request.RequestingProfessionalId,
            Modality = request.Modality,
            StudyDescription = request.StudyDescription.Trim(),
            ScheduledAt = scheduledAt,
            AccessionNumber = $"IMG-{DateTime.UtcNow:yyyyMM}-{(count + 1):D5}"
        };

        dbContext.ImagingStudies.Add(study);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(study.Id, cancellationToken))!;
    }

    public async Task<ImagingStudyDto?> UpdateStatusAsync(
        Guid id, UpdateImagingStudyStatusRequest request, CancellationToken cancellationToken = default)
    {
        var study = await dbContext.ImagingStudies.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (study is null)
        {
            return null;
        }

        study.Status = request.Status;
        study.UpdatedAt = DateTime.UtcNow;

        if (request.Status == ImagingStudyStatus.Completed)
        {
            study.CompletedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ImagingStudyDto?> RegisterReportAsync(
        Guid id, RegisterImagingReportRequest request, CancellationToken cancellationToken = default)
    {
        var study = await dbContext.ImagingStudies.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (study is null)
        {
            return null;
        }

        study.ReportContent = request.ReportContent.Trim();
        study.ReportingProfessionalId = request.ReportingProfessionalId;
        study.ReportedAt = DateTime.UtcNow;
        study.Status = ImagingStudyStatus.Completed;
        study.CompletedAt ??= DateTime.UtcNow;
        study.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task<ImagingStudyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.ImagingStudies
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(MapStudy())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static System.Linq.Expressions.Expression<Func<ImagingStudy, ImagingStudyDto>> MapStudy() =>
        s => new ImagingStudyDto(
            s.Id, s.PatientId, s.Patient.FullName, s.RequestingProfessional.FullName,
            s.Modality, s.StudyDescription, s.Status, s.ScheduledAt, s.CompletedAt,
            s.ReportContent, s.ReportedAt, s.AccessionNumber);
}
