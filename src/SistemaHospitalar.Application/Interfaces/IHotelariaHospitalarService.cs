using SistemaHospitalar.Application.DTOs.Hotelaria;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IHotelariaHospitalarService
{
    Task<HotelariaNocDto> GetNocDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CleaningRequestDto>> GetCleaningRequestsAsync(
        CleaningRequestStatus? status, CancellationToken cancellationToken = default);
    Task<CleaningRequestDto> CreateCleaningRequestAsync(
        CreateCleaningRequestRequest request, CancellationToken cancellationToken = default);
    Task<CleaningRequestDto?> StartCleaningAsync(
        Guid id, StartCleaningRequestRequest request, CancellationToken cancellationToken = default);
    Task<CleaningRequestDto?> UpdateChecklistAsync(
        Guid id, UpdateCleaningChecklistRequest request, CancellationToken cancellationToken = default);
    Task<CleaningRequestDto?> CompleteCleaningAsync(
        Guid id, CompleteCleaningRequestRequest request, CancellationToken cancellationToken = default);
    Task<CleaningRequestDto?> CancelCleaningAsync(Guid id, CancellationToken cancellationToken = default);
    Task RequestBedCleaningAsync(
        Guid bedId,
        Guid? hospitalizationId,
        CleaningType cleaningType,
        CleaningTriggerReason triggerReason,
        CancellationToken cancellationToken = default);
}
