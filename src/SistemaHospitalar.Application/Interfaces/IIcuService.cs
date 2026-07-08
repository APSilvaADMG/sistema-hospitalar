using SistemaHospitalar.Application.DTOs.Icu;

namespace SistemaHospitalar.Application.Interfaces;

public interface IIcuService
{
    Task<IcuDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<VitalSignDto> RecordVitalSignsAsync(RecordVitalSignsRequest request, CancellationToken cancellationToken = default);
    Task<VitalSignDto> RecordVitalSignCorrectionAsync(
        RecordVitalSignCorrectionRequest request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VitalSignDto>> GetVitalHistoryAsync(Guid hospitalizationId, int limit, CancellationToken cancellationToken = default);
}
