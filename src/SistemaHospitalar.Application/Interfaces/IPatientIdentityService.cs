using SistemaHospitalar.Application.DTOs.PatientIdentity;

namespace SistemaHospitalar.Application.Interfaces;

public interface IPatientIdentityService
{
    Task<PatientIdentityDto> GenerateBraceletAsync(
        Guid patientId,
        GenerateBraceletRequest request,
        Guid? issuedByUserId,
        CancellationToken cancellationToken = default);

    Task<PatientIdentityDto> GenerateLabelAsync(
        Guid patientId,
        GenerateLabelRequest request,
        Guid? issuedByUserId,
        CancellationToken cancellationToken = default);

    Task<PatientIdentityResolveDto?> ResolveAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientIdentityDto>> ListActiveAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);
}
