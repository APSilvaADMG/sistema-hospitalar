using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Employee> Employees { get; set; } = [];
}

public class Employee : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? SocialName { get; set; }
    public string? Cpf { get; set; }
    public string? Rg { get; set; }
    public DateOnly? BirthDate { get; set; }
    public Gender Gender { get; set; } = Gender.NotInformed;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? JobTitle { get; set; }
    public EmployeeRole Role { get; set; }
    public DateOnly HireDate { get; set; }
    public string? AddressStreet { get; set; }
    public string? AddressNumber { get; set; }
    public string? AddressComplement { get; set; }
    public string? AddressNeighborhood { get; set; }
    public string? AddressCity { get; set; }
    public string? AddressState { get; set; }
    public string? AddressZipCode { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Notes { get; set; }
    public string? PayrollNotes { get; set; }
    public string? PhotoData { get; set; }
    public decimal BaseSalary { get; set; }

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public ICollection<EmployeeShift> Shifts { get; set; } = [];
    public ICollection<EmployeeHrEvent> HrEvents { get; set; } = [];
}

public class EmployeeHrEvent : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public HrEventType EventType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
}

public class EmployeeShift : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public DateOnly ShiftDate { get; set; }
    public ShiftType ShiftType { get; set; }
}
