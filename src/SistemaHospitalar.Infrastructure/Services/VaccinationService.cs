using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.ClinicalOperations;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class VaccinationService(AppDbContext dbContext) : IVaccinationService
{
    public async Task<IReadOnlyList<VaccineCatalogDto>> ListCatalogAsync(
        VaccineScheduleType? scheduleType = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.VaccineCatalogs.AsNoTracking().Where(v => v.IsActive);
        if (scheduleType.HasValue)
        {
            query = query.Where(v => v.ScheduleType == scheduleType.Value);
        }

        return await query
            .OrderBy(v => v.DisplayOrder)
            .Select(v => new VaccineCatalogDto(v.Id, v.Code, v.Name, v.ScheduleType, v.DisplayOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PatientVaccinationDto>> ListByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PatientVaccinations
            .AsNoTracking()
            .Where(v => v.PatientId == patientId && v.IsActive)
            .OrderByDescending(v => v.AdministeredAt)
            .Select(v => new PatientVaccinationDto(
                v.Id,
                v.PatientId,
                v.Patient.FullName,
                v.VaccineCatalogId,
                v.VaccineCatalog.Name,
                v.VaccineCatalog.Code,
                v.AdministeredAt,
                v.DoseNumber,
                v.BatchNumber,
                v.ProfessionalId,
                v.Professional != null ? v.Professional.FullName : null,
                v.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<PatientVaccinationDto> CreateAsync(
        CreatePatientVaccinationRequest request,
        CancellationToken cancellationToken = default)
    {
        var patientExists = await dbContext.Patients.AnyAsync(
            p => p.Id == request.PatientId && p.IsActive,
            cancellationToken);
        if (!patientExists)
        {
            throw new InvalidOperationException("Paciente não encontrado.");
        }

        var vaccine = await dbContext.VaccineCatalogs
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VaccineCatalogId && v.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Vacina não encontrada no catálogo.");

        if (request.ProfessionalId.HasValue)
        {
            var professionalExists = await dbContext.Professionals.AnyAsync(
                p => p.Id == request.ProfessionalId && p.IsActive,
                cancellationToken);
            if (!professionalExists)
            {
                throw new InvalidOperationException("Profissional não encontrado.");
            }
        }

        var entity = new PatientVaccination
        {
            PatientId = request.PatientId,
            VaccineCatalogId = request.VaccineCatalogId,
            AdministeredAt = request.AdministeredAt.ToUniversalTime(),
            DoseNumber = Math.Max(1, request.DoseNumber),
            BatchNumber = string.IsNullOrWhiteSpace(request.BatchNumber) ? null : request.BatchNumber.Trim(),
            ProfessionalId = request.ProfessionalId,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
        };

        dbContext.PatientVaccinations.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await ListByPatientAsync(request.PatientId, cancellationToken))
            .First(v => v.Id == entity.Id);
    }

    public async Task<IReadOnlyList<EpidemicDiseaseCatalogDto>> ListEpidemicDiseasesAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.EpidemicDiseaseCatalogs.AsNoTracking().Where(d => d.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d => d.Name.Contains(term) || d.Code.Contains(term));
        }

        return await query
            .OrderBy(d => d.DisplayOrder)
            .Select(d => new EpidemicDiseaseCatalogDto(
                d.Id, d.Code, d.Name, d.DiseaseClass, d.IncludeOpd, d.IncludeIpd, d.DisplayOrder))
            .ToListAsync(cancellationToken);
    }
}
