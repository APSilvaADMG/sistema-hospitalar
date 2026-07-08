using SistemaHospitalar.Application.DTOs.Administrative;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IAdministrativeExtensionsService
{
    Task<IReadOnlyList<TpaAdministratorDto>> GetTpaAdministratorsAsync(CancellationToken cancellationToken = default);
    Task<TpaAdministratorDto> CreateTpaAdministratorAsync(CreateTpaAdministratorRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TpaClaimDto>> GetTpaClaimsAsync(Guid? administratorId = null, TpaClaimStatus? status = null, CancellationToken cancellationToken = default);
    Task<TpaClaimDto> CreateTpaClaimAsync(CreateTpaClaimRequest request, CancellationToken cancellationToken = default);
    Task<TpaClaimDto?> UpdateTpaClaimStatusAsync(Guid claimId, UpdateTpaClaimStatusRequest request, CancellationToken cancellationToken = default);
    Task<TpaReportDto> GetTpaReportAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PayrollRunDto>> GetPayrollRunsAsync(CancellationToken cancellationToken = default);
    Task<PayrollRunDto?> GetPayrollRunAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<PayrollRunDto> GeneratePayrollRunAsync(GeneratePayrollRunRequest request, CancellationToken cancellationToken = default);
    Task<PayrollRunDto?> UpdatePayrollRunStatusAsync(Guid runId, UpdatePayrollRunStatusRequest request, CancellationToken cancellationToken = default);
    Task<PayrollItemDto?> UpdatePayrollItemLinesAsync(Guid runId, Guid itemId, UpdatePayrollItemLinesRequest request, CancellationToken cancellationToken = default);
    Task<PayrollSlipDto?> GetPayrollSlipAsync(Guid runId, Guid employeeId, CancellationToken cancellationToken = default);
    Task<PayrollMonthlySummaryDto> GetPayrollMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PharmacyBillingEntryDto>> GetPharmacyBillingEntriesAsync(bool? paid = null, CancellationToken cancellationToken = default);
    Task<PharmacyBillingEntryDto> CreatePharmacyBillingEntryAsync(CreatePharmacyBillingEntryRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BirthRegistrationDto>> GetBirthRegistrationsAsync(CancellationToken cancellationToken = default);
    Task<BirthRegistrationDto> CreateBirthRegistrationAsync(CreateBirthRegistrationRequest request, CancellationToken cancellationToken = default);
}

