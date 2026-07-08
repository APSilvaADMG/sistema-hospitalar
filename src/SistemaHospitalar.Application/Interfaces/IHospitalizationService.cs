using SistemaHospitalar.Application.DTOs.Hospitalization;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IHospitalizationService
{
    Task<IReadOnlyList<WardDto>> GetWardsAsync(
        WardCoverageModality? modality = null,
        WardCategory? category = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BedDto>> GetBedsAsync(
        Guid? wardId = null,
        WardCoverageModality? modality = null,
        WardCategory? category = null,
        BedStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BedDto>> GetAvailableBedsForPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HospitalizationDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HospitalizationDto>> GetListAsync(
        HospitalizationListScope scope = HospitalizationListScope.Active,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HospitalizationDto>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<HospitalizationDto> AdmitAsync(AdmitPatientRequest request, CancellationToken cancellationToken = default);
    Task<HospitalizationDto?> DischargeAsync(Guid id, DischargePatientRequest request, CancellationToken cancellationToken = default);
    Task<HospitalizationDto?> RegisterPatientDeathAsync(
        Guid id,
        RegisterPatientDeathRequest request,
        CancellationToken cancellationToken = default);
    Task<HospitalizationDto> TransferBedAsync(Guid hospitalizationId, TransferBedRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BedTransferDto>> GetBedTransfersAsync(int? limit, CancellationToken cancellationToken = default);

    Task<WardDto> CreateWardAsync(CreateWardRequest request, CancellationToken cancellationToken = default);
    Task<WardDto?> UpdateWardAsync(Guid id, UpdateWardRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeactivateWardAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BedDto> CreateBedAsync(CreateBedRequest request, CancellationToken cancellationToken = default);
    Task<BedDto?> UpdateBedAsync(Guid id, UpdateBedRequest request, CancellationToken cancellationToken = default);
    Task<BedDto> UpdateBedStatusAsync(Guid id, UpdateBedStatusRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeactivateBedAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HospitalizationRequestDto>> GetHospitalizationRequestsAsync(
        HospitalizationRequestStatus? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default);

    Task<HospitalizationRequestDto> CreateHospitalizationRequestAsync(
        CreateHospitalizationRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<HospitalizationRequestDto> ReviewHospitalizationRequestAsync(
        Guid id,
        ReviewHospitalizationRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<HospitalizationRequestDto> CancelHospitalizationRequestAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<HospitalizationDto> AdmitFromHospitalizationRequestAsync(
        Guid requestId,
        AdmitFromHospitalizationRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<HospitalizationDto> UpdateSusDataAsync(
        Guid hospitalizationId,
        UpdateHospitalizationSusDataRequest request,
        CancellationToken cancellationToken = default);

    Task<HospitalizationDto?> CloseBillingAccountAsync(
        Guid hospitalizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HospitalizationSnippetDto>> GetSnippetsAsync(
        HospitalizationSnippetType type,
        CancellationToken cancellationToken = default);

    Task<HospitalizationSnippetDto> RegisterSnippetAsync(
        RegisterHospitalizationSnippetRequest request,
        Guid? userId,
        CancellationToken cancellationToken = default);

    Task<BedEventDto> ReserveBedAsync(Guid bedId, ReserveBedRequest request, CancellationToken cancellationToken = default);
    Task<BedEventDto> BlockBedAsync(Guid bedId, BlockBedRequest request, CancellationToken cancellationToken = default);
    Task<BedEventDto> ReleaseBedAsync(Guid bedId, ReleaseBedRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BedEventDto>> GetBedEventsAsync(
        Guid? bedId,
        bool activeOnly,
        CancellationToken cancellationToken = default);
}
