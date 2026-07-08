using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.HumanResources;

public record DepartmentDto(Guid Id, string Name, string? Description, int EmployeeCount);

public record EmployeeDto(
    Guid Id,
    string FullName,
    string? Email,
    string? JobTitle,
    EmployeeRole Role,
    string DepartmentName,
    DateOnly HireDate,
    decimal BaseSalary,
    bool HasPhoto);

public record EmployeeDetailDto(
    Guid Id,
    string FullName,
    string? SocialName,
    string? Cpf,
    string? Rg,
    DateOnly? BirthDate,
    Gender Gender,
    string? Email,
    string? Phone,
    string? MobilePhone,
    string? JobTitle,
    EmployeeRole Role,
    Guid DepartmentId,
    string DepartmentName,
    DateOnly HireDate,
    string? AddressStreet,
    string? AddressNumber,
    string? AddressComplement,
    string? AddressNeighborhood,
    string? AddressCity,
    string? AddressState,
    string? AddressZipCode,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes,
    string? PhotoData,
    decimal BaseSalary,
    bool IsActive,
    DateTime CreatedAt);

public record EmployeeShiftDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string DepartmentName,
    DateOnly ShiftDate,
    ShiftType ShiftType);

public record CreateEmployeeRequest(
    string FullName,
    string? SocialName,
    string? Cpf,
    string? Rg,
    DateOnly? BirthDate,
    Gender Gender,
    string? Email,
    string? Phone,
    string? MobilePhone,
    string? JobTitle,
    EmployeeRole Role,
    Guid DepartmentId,
    DateOnly HireDate,
    string? AddressStreet,
    string? AddressNumber,
    string? AddressComplement,
    string? AddressNeighborhood,
    string? AddressCity,
    string? AddressState,
    string? AddressZipCode,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes,
    string? PhotoData,
    decimal BaseSalary);

public record UpdateEmployeeRequest(
    string FullName,
    string? SocialName,
    string? Cpf,
    string? Rg,
    DateOnly? BirthDate,
    Gender Gender,
    string? Email,
    string? Phone,
    string? MobilePhone,
    string? JobTitle,
    EmployeeRole Role,
    Guid DepartmentId,
    DateOnly HireDate,
    string? AddressStreet,
    string? AddressNumber,
    string? AddressComplement,
    string? AddressNeighborhood,
    string? AddressCity,
    string? AddressState,
    string? AddressZipCode,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes,
    string? PhotoData,
    decimal BaseSalary,
    bool IsActive);

public record HrDashboardDto(
    int ActiveEmployees,
    int ShiftsThisWeek,
    int NightShiftsThisWeek,
    int OnVacationToday,
    int TrainingsThisMonth,
    int ReviewsThisQuarter,
    decimal? LatestPayrollNet,
    int PayrollEmployeeCount);

public record CreateShiftRequest(
    Guid EmployeeId,
    Guid DepartmentId,
    DateOnly ShiftDate,
    ShiftType ShiftType);

public record EmployeeHrEventDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    HrEventType EventType,
    string Title,
    string Detail,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Notes);

public record CreateEmployeeHrEventRequest(
    Guid EmployeeId,
    HrEventType EventType,
    string Title,
    string Detail,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Notes);
