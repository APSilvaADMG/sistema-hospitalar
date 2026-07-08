using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Application.DTOs.Guides;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class SusGuideService(
    AppDbContext dbContext,
    GuideAuditLogger auditLogger,
    IConnectNotificationService connectNotificationService) : ISusGuideService
{
    public async Task<IReadOnlyList<SusGuideDto>> SearchAsync(
        SusGuideFilterDto filter, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(filter.Take, 1, 200);
        var skip = Math.Max(filter.Skip, 0);
        return await BuildQuery(filter)
            .OrderByDescending(g => g.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(Map())
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(SusGuideFilterDto filter, CancellationToken cancellationToken = default)
        => BuildQuery(filter).CountAsync(cancellationToken);

    public Task<SusGuideDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.SusGuides.AsNoTracking()
            .Where(g => g.Id == id && g.IsActive)
            .Select(Map())
            .FirstOrDefaultAsync(cancellationToken)!;

    public async Task<SusGuideDto> CreateAsync(
        CreateSusGuideRequest request, CancellationToken cancellationToken = default)
    {
        var guideType = (SusGuideType)request.GuideType;
        var guideNumber = await NextGuideNumberAsync(guideType, cancellationToken);
        var competence = request.Competence?.Trim()
            ?? DateTime.UtcNow.ToString("yyyyMM");

        var guide = new SusGuide
        {
            GuideNumber = guideNumber,
            GuideType = guideType,
            Status = SusGuideStatus.Draft,
            PatientId = request.PatientId,
            ProfessionalId = request.ProfessionalId,
            ServiceUnitId = request.ServiceUnitId ?? await DefaultServiceUnitIdAsync(cancellationToken),
            AppointmentId = request.AppointmentId,
            HospitalizationId = request.HospitalizationId,
            Cid10Code = request.Cid10Code?.Trim(),
            SigtapProcedureCode = request.SigtapProcedureCode?.Trim(),
            ProcedureDescription = request.ProcedureDescription?.Trim(),
            Competence = competence,
            Notes = request.Notes?.Trim(),
            TotalAmount = request.TotalAmount,
        };

        dbContext.SusGuides.Add(guide);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.LogSusGuideAsync(
            guide.Id, "Emissão", $"Guia SUS {guide.GuideNumber} criada.", cancellationToken: cancellationToken);

        return (await GetByIdAsync(guide.Id, cancellationToken))!;
    }

    public async Task<SusGuideDto?> UpdateAsync(
        Guid id, UpdateSusGuideRequest request, CancellationToken cancellationToken = default)
    {
        var guide = await dbContext.SusGuides.FirstOrDefaultAsync(g => g.Id == id && g.IsActive, cancellationToken);
        if (guide is null || guide.Status != SusGuideStatus.Draft)
        {
            return null;
        }

        guide.ProfessionalId = request.ProfessionalId;
        guide.ServiceUnitId = request.ServiceUnitId ?? guide.ServiceUnitId;
        guide.AppointmentId = request.AppointmentId;
        guide.HospitalizationId = request.HospitalizationId;
        guide.Cid10Code = request.Cid10Code?.Trim();
        guide.SigtapProcedureCode = request.SigtapProcedureCode?.Trim();
        guide.ProcedureDescription = request.ProcedureDescription?.Trim();
        guide.Competence = request.Competence?.Trim() ?? guide.Competence;
        guide.Notes = request.Notes?.Trim();
        guide.TotalAmount = request.TotalAmount;
        guide.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.LogSusGuideAsync(guide.Id, "Atualização", "Guia SUS atualizada.", cancellationToken: cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<SusGuideDto?> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var guide = await dbContext.SusGuides.FirstOrDefaultAsync(g => g.Id == id && g.IsActive, cancellationToken);
        if (guide is null || guide.Status is SusGuideStatus.Billed or SusGuideStatus.Cancelled)
        {
            return null;
        }

        var before = guide.Status;
        guide.Status = SusGuideStatus.Cancelled;
        guide.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.LogSusGuideAsync(
            guide.Id, "Cancelamento", "Guia SUS cancelada.", before, SusGuideStatus.Cancelled, cancellationToken);

        var billingUserIds = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role == UserRole.Billing)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var billingUserId in billingUserIds)
        {
            await connectNotificationService.CreateAsync(new CreateConnectNotificationRequest(
                billingUserId,
                "Guia SUS cancelada",
                $"A guia {guide.GuideNumber} foi cancelada.",
                ConnectNotificationCategory.Alert,
                nameof(SusGuide),
                guide.Id), cancellationToken);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<SusGuideDto?> SubmitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var guide = await dbContext.SusGuides.FirstOrDefaultAsync(g => g.Id == id && g.IsActive, cancellationToken);
        if (guide is null || guide.Status != SusGuideStatus.Draft)
        {
            return null;
        }

        var before = guide.Status;
        guide.Status = SusGuideStatus.Submitted;
        guide.SubmittedAt = DateTime.UtcNow;
        guide.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.LogSusGuideAsync(
            guide.Id, "Envio", "Guia SUS enviada para autorização.", before, SusGuideStatus.Submitted, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<SusGuideDto?> AuthorizeAsync(
        Guid id, string? authorizationNumber, CancellationToken cancellationToken = default)
    {
        var guide = await dbContext.SusGuides.FirstOrDefaultAsync(g => g.Id == id && g.IsActive, cancellationToken);
        if (guide is null || guide.Status is not (SusGuideStatus.Draft or SusGuideStatus.Submitted))
        {
            return null;
        }

        var before = guide.Status;
        guide.Status = SusGuideStatus.Authorized;
        guide.AuthorizationNumber = authorizationNumber?.Trim() ?? $"AUT{DateTime.UtcNow:yyyyMMddHHmm}";
        guide.AuthorizedAt = DateTime.UtcNow;
        guide.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLogger.LogSusGuideAsync(
            guide.Id, "Autorização", $"Guia autorizada: {guide.AuthorizationNumber}", before, SusGuideStatus.Authorized, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<SusGuideDto?> DuplicateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var source = await dbContext.SusGuides.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id && g.IsActive, cancellationToken);
        if (source is null)
        {
            return null;
        }

        return await CreateAsync(new CreateSusGuideRequest(
            source.PatientId,
            (int)source.GuideType,
            source.ProfessionalId,
            source.ServiceUnitId,
            source.AppointmentId,
            source.HospitalizationId,
            source.Cid10Code,
            source.SigtapProcedureCode,
            source.ProcedureDescription,
            source.Competence,
            source.Notes,
            source.TotalAmount), cancellationToken);
    }

    private IQueryable<SusGuide> BuildQuery(SusGuideFilterDto filter)
    {
        var query = dbContext.SusGuides.AsNoTracking().Where(g => g.IsActive);
        var (from, to) = NormalizeRange(filter.DateFrom, filter.DateTo);
        query = query.Where(g => g.CreatedAt >= from && g.CreatedAt <= to);

        if (filter.PatientId.HasValue)
        {
            query = query.Where(g => g.PatientId == filter.PatientId.Value);
        }

        if (filter.ProfessionalId.HasValue)
        {
            query = query.Where(g => g.ProfessionalId == filter.ProfessionalId.Value);
        }

        if (filter.ServiceUnitId.HasValue)
        {
            query = query.Where(g => g.ServiceUnitId == filter.ServiceUnitId.Value);
        }

        if (filter.GuideType.HasValue)
        {
            query = query.Where(g => (int)g.GuideType == filter.GuideType.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(g => (int)g.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.GuideNumber))
        {
            var term = filter.GuideNumber.Trim();
            query = query.Where(g => g.GuideNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.ProcedureSearch))
        {
            var term = filter.ProcedureSearch.Trim();
            query = query.Where(g =>
                (g.ProcedureDescription != null && g.ProcedureDescription.Contains(term))
                || (g.SigtapProcedureCode != null && g.SigtapProcedureCode.Contains(term)));
        }

        return query;
    }

    private async Task<string> NextGuideNumberAsync(SusGuideType type, CancellationToken cancellationToken)
    {
        var prefix = type switch
        {
            SusGuideType.Bpa => "SUS-BPA",
            SusGuideType.Apac => "SUS-APAC",
            SusGuideType.Aih => "SUS-AIH",
            _ => "SUS",
        };
        var count = await dbContext.SusGuides.CountAsync(g => g.GuideType == type, cancellationToken);
        return $"{prefix}-{DateTime.UtcNow:yyyy}-{(count + 1):D6}";
    }

    private async Task<Guid?> DefaultServiceUnitIdAsync(CancellationToken cancellationToken)
        => await dbContext.ServiceUnits.AsNoTracking()
            .Where(u => u.IsActive && u.IsDefault)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private static (DateTime From, DateTime To) NormalizeRange(DateTime? from, DateTime? to)
    {
        var end = to.HasValue
            ? AsUtcDateEnd(to.Value)
            : DateTime.UtcNow;
        var start = from.HasValue
            ? AsUtcDateStart(from.Value)
            : end.AddDays(-30);
        if (start > end)
        {
            (start, end) = (end, start);
        }

        return (start, end);
    }

    private static DateTime AsUtcDateStart(DateTime value)
        => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    private static DateTime AsUtcDateEnd(DateTime value)
        => DateTime.SpecifyKind(value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

    private static System.Linq.Expressions.Expression<Func<SusGuide, SusGuideDto>> Map() =>
        g => new SusGuideDto(
            g.Id,
            g.GuideNumber,
            (int)g.GuideType,
            (int)g.Status,
            g.PatientId,
            g.Patient.FullName,
            g.ProfessionalId,
            g.Professional != null ? g.Professional.FullName : null,
            g.ServiceUnitId,
            g.ServiceUnit != null ? g.ServiceUnit.Name : null,
            g.AppointmentId,
            g.HospitalizationId,
            g.Cid10Code,
            g.SigtapProcedureCode,
            g.ProcedureDescription,
            g.Competence,
            g.AuthorizationNumber,
            g.AuthorizedAt,
            g.SubmittedAt,
            g.TotalAmount,
            g.Notes,
            g.CreatedAt);
}
