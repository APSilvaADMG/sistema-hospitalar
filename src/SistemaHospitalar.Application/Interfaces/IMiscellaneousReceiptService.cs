using SistemaHospitalar.Application.Common;
using SistemaHospitalar.Application.DTOs.Financial;

namespace SistemaHospitalar.Application.Interfaces;

public interface IMiscellaneousReceiptService
{
    Task<PagedResult<MiscellaneousReceiptDto>> SearchAsync(
        string? search,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<MiscellaneousReceiptDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MiscellaneousReceiptDto> CreateAsync(
        CreateMiscellaneousReceiptRequest request,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    Task<MiscellaneousReceiptDto?> UpdateAsync(
        Guid id,
        UpdateMiscellaneousReceiptRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
