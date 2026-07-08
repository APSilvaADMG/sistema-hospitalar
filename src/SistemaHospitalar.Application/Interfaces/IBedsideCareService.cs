using SistemaHospitalar.Application.DTOs.Bedside;

namespace SistemaHospitalar.Application.Interfaces;

public interface IBedsideCareService
{
    Task<BedsideCareResultDto?> RegisterVitalsAsync(
        Guid patientId,
        BedsideVitalsRequest request,
        Guid userId,
        string userEmail,
        Guid? professionalId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<BedsideCareResultDto?> AdministerMedicationAsync(
        Guid patientId,
        BedsideMedicationRequest request,
        Guid userId,
        string userEmail,
        Guid professionalId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
