using SistemaHospitalar.Application.DTOs.HumanResources;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IHrService
{
    Task<HrDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(CancellationToken cancellationToken = default);
    Task<EmployeeDetailDto?> GetEmployeeByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmployeeDetailDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDetailDto?> UpdateEmployeeAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeShiftDto>> GetShiftsAsync(
        DateOnly? date,
        DateOnly? from,
        DateOnly? to,
        ShiftType? shiftType,
        CancellationToken cancellationToken = default);
    Task<EmployeeShiftDto> CreateShiftAsync(CreateShiftRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteShiftAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeHrEventDto>> GetHrEventsAsync(HrEventType? eventType, CancellationToken cancellationToken = default);
    Task<EmployeeHrEventDto> CreateHrEventAsync(CreateEmployeeHrEventRequest request, CancellationToken cancellationToken = default);
}
