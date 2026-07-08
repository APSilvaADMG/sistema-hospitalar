using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Catalog;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class ProfessionalService(AppDbContext dbContext) : IProfessionalService
{
    public async Task<IReadOnlyList<ProfessionalListDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Professionals
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.FullName)
            .Select(p => new ProfessionalListDto(
                p.Id,
                p.FullName,
                p.Crm,
                p.CouncilUf,
                p.SpecialtyId,
                p.Specialty.Name,
                p.PhotoData != null))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProfessionalDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Professionals
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => MapDetail(p))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProfessionalDetailDto> CreateAsync(
        CreateProfessionalRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.Cpf))
        {
            var cpf = NormalizeDigits(request.Cpf);
            if (await dbContext.Professionals.AnyAsync(p => p.Cpf == cpf, cancellationToken))
            {
                throw new InvalidOperationException("Já existe um profissional cadastrado com este CPF.");
            }
        }

        var professional = MapToEntity(new Professional(), request);
        dbContext.Professionals.Add(professional);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(professional.Id, cancellationToken))!;
    }

    public async Task<ProfessionalDetailDto?> UpdateAsync(
        Guid id,
        UpdateProfessionalRequest request,
        CancellationToken cancellationToken = default)
    {
        var professional = await dbContext.Professionals.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (professional is null)
        {
            return null;
        }

        professional.FullName = request.FullName.Trim();
        professional.SocialName = request.SocialName?.Trim();
        professional.Crm = request.Crm?.Trim();
        professional.CouncilUf = request.CouncilUf?.Trim()?.ToUpperInvariant();
        professional.Cpf = string.IsNullOrWhiteSpace(request.Cpf) ? null : NormalizeDigits(request.Cpf);
        professional.Rg = request.Rg?.Trim();
        professional.BirthDate = request.BirthDate;
        professional.Gender = request.Gender;
        professional.Email = request.Email?.Trim();
        professional.Phone = request.Phone?.Trim();
        professional.MobilePhone = request.MobilePhone?.Trim();
        professional.AddressStreet = request.AddressStreet?.Trim();
        professional.AddressNumber = request.AddressNumber?.Trim();
        professional.AddressComplement = request.AddressComplement?.Trim();
        professional.AddressNeighborhood = request.AddressNeighborhood?.Trim();
        professional.AddressCity = request.AddressCity?.Trim();
        professional.AddressState = request.AddressState?.Trim()?.ToUpperInvariant();
        professional.AddressZipCode = request.AddressZipCode?.Trim();
        professional.Notes = request.Notes?.Trim();
        professional.SpecialtyId = request.SpecialtyId;
        professional.IsActive = request.IsActive;
        professional.UpdatedAt = DateTime.UtcNow;

        if (request.PhotoData is not null)
        {
            professional.PhotoData = NormalizePhoto(request.PhotoData);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    private static Professional MapToEntity(Professional professional, CreateProfessionalRequest request)
    {
        professional.FullName = request.FullName.Trim();
        professional.SocialName = request.SocialName?.Trim();
        professional.Crm = request.Crm?.Trim();
        professional.CouncilUf = request.CouncilUf?.Trim()?.ToUpperInvariant();
        professional.Cpf = string.IsNullOrWhiteSpace(request.Cpf) ? null : NormalizeDigits(request.Cpf);
        professional.Rg = request.Rg?.Trim();
        professional.BirthDate = request.BirthDate;
        professional.Gender = request.Gender;
        professional.Email = request.Email?.Trim();
        professional.Phone = request.Phone?.Trim();
        professional.MobilePhone = request.MobilePhone?.Trim();
        professional.AddressStreet = request.AddressStreet?.Trim();
        professional.AddressNumber = request.AddressNumber?.Trim();
        professional.AddressComplement = request.AddressComplement?.Trim();
        professional.AddressNeighborhood = request.AddressNeighborhood?.Trim();
        professional.AddressCity = request.AddressCity?.Trim();
        professional.AddressState = request.AddressState?.Trim()?.ToUpperInvariant();
        professional.AddressZipCode = request.AddressZipCode?.Trim();
        professional.Notes = request.Notes?.Trim();
        professional.PhotoData = NormalizePhoto(request.PhotoData);
        professional.SpecialtyId = request.SpecialtyId;
        return professional;
    }

    private static ProfessionalDetailDto MapDetail(Professional p) => new(
        p.Id,
        p.FullName,
        p.SocialName,
        p.Crm,
        p.CouncilUf,
        p.Cpf,
        p.Rg,
        p.BirthDate,
        p.Gender,
        p.Email,
        p.Phone,
        p.MobilePhone,
        p.AddressStreet,
        p.AddressNumber,
        p.AddressComplement,
        p.AddressNeighborhood,
        p.AddressCity,
        p.AddressState,
        p.AddressZipCode,
        p.Notes,
        p.PhotoData,
        p.SpecialtyId,
        p.Specialty.Name,
        p.IsActive,
        p.CreatedAt);

    private static string NormalizeDigits(string value) =>
        new string(value.Where(char.IsDigit).ToArray());

    private static string? NormalizePhoto(string? photoData)
    {
        if (string.IsNullOrWhiteSpace(photoData))
        {
            return null;
        }

        if (photoData.Length > 500_000)
        {
            throw new InvalidOperationException("A foto excede o tamanho máximo permitido.");
        }

        return photoData;
    }
}
