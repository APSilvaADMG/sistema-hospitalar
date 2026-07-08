using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.Security;

namespace SistemaHospitalar.Infrastructure.Services;

internal static class PatientCareValidation
{
    public static async Task<Patient> RequireEligibleForCareAsync(
        AppDbContext dbContext,
        Guid patientId,
        IFieldEncryptionService? encryption,
        bool validateSusCns,
        CancellationToken cancellationToken)
    {
        var patient = await dbContext.Patients
            .Include(p => p.Insurances.Where(i => i.IsActive))
                .ThenInclude(i => i.HealthInsurance)
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken)
            ?? throw new InvalidOperationException("Paciente não encontrado.");

        if (encryption is not null)
        {
            PatientFieldProtection.Decrypt(patient, encryption);
        }

        HospitalBusinessRules.ValidateEligibleForCare(patient.IsActive, patient.IsDeceased);

        if (validateSusCns)
        {
            var primary = patient.Insurances
                .Where(i => i.IsActive)
                .OrderByDescending(i => i.IsPrimary)
                .FirstOrDefault();

            HospitalBusinessRules.ValidateCnsForSus(
                primary?.HealthInsurance?.Name,
                patient.Cns,
                primary?.CnsNumber);
        }

        return patient;
    }
}
