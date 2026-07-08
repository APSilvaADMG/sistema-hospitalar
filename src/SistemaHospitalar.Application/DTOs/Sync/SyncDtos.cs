using System.Text.Json;
using SistemaHospitalar.Application.DTOs.Hotelaria;
using SistemaHospitalar.Application.DTOs.Transport;

namespace SistemaHospitalar.Application.DTOs.Sync;

public record SyncMutationItemDto(
    Guid ClientMutationId,
    string Entity,
    string Action,
    JsonElement Payload,
    DateTime ClientTimestamp);

public record SyncPushRequest(
    string DeviceId,
    IReadOnlyList<SyncMutationItemDto> Mutations);

public record SyncMutationResultDto(
    Guid ClientMutationId,
    string Status,
    string? Message,
    JsonElement? ServerPayload);

public record SyncPushResponse(
    DateTime ServerTimestamp,
    IReadOnlyList<SyncMutationResultDto> Results);

public record SyncPullRequest(
    DateTime? Since,
    string? Sector,
    Guid? WardId);

public record BedSyncDto(
    Guid Id,
    Guid WardId,
    string WardName,
    string BedNumber,
    string Status,
    string? StatusReason,
    DateTime UpdatedAt);

public record SyncPullResponse(
    DateTime ServerTimestamp,
    IReadOnlyList<TransportRequestDto> TransportRequests,
    IReadOnlyList<CleaningRequestDto> CleaningRequests,
    IReadOnlyList<TransportAssetDto> TransportAssets,
    IReadOnlyList<TransportPorterDto> Porters,
    IReadOnlyList<BedSyncDto> Beds);
