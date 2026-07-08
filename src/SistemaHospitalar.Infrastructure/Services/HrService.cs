using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.HumanResources;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class HrService(AppDbContext dbContext) : IHrService
{
    public async Task<HrDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (weekStart, weekEnd) = GetWeekRange(today);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var quarterMonth = ((today.Month - 1) / 3) * 3 + 1;
        var quarterStart = new DateOnly(today.Year, quarterMonth, 1);
        var quarterEnd = quarterStart.AddMonths(3).AddDays(-1);

        var activeEmployees = await dbContext.Employees
            .CountAsync(e => e.IsActive, cancellationToken);

        var shiftsThisWeek = await dbContext.EmployeeShifts
            .CountAsync(s => s.IsActive && s.ShiftDate >= weekStart && s.ShiftDate <= weekEnd, cancellationToken);

        var nightShiftsThisWeek = await dbContext.EmployeeShifts
            .CountAsync(s => s.IsActive
                && s.ShiftDate >= weekStart
                && s.ShiftDate <= weekEnd
                && s.ShiftType == ShiftType.Night, cancellationToken);

        var onVacationToday = await dbContext.EmployeeHrEvents
            .CountAsync(e => e.IsActive
                && e.EventType == HrEventType.Vacation
                && e.StartDate <= today
                && (e.EndDate == null || e.EndDate >= today), cancellationToken);

        var trainingsThisMonth = await dbContext.EmployeeHrEvents
            .CountAsync(e => e.IsActive
                && e.EventType == HrEventType.Training
                && e.StartDate >= monthStart
                && e.StartDate <= monthEnd, cancellationToken);

        var reviewsThisQuarter = await dbContext.EmployeeHrEvents
            .CountAsync(e => e.IsActive
                && e.EventType == HrEventType.PerformanceReview
                && e.StartDate >= quarterStart
                && e.StartDate <= quarterEnd, cancellationToken);

        var latestPayroll = await dbContext.PayrollRuns
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Month)
            .Select(r => new { r.TotalNet, EmployeeCount = r.Items.Count(i => i.IsActive) })
            .FirstOrDefaultAsync(cancellationToken);

        return new HrDashboardDto(
            activeEmployees,
            shiftsThisWeek,
            nightShiftsThisWeek,
            onVacationToday,
            trainingsThisMonth,
            reviewsThisQuarter,
            latestPayroll?.TotalNet,
            latestPayroll?.EmployeeCount ?? 0);
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Departments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.Description,
                d.Employees.Count(e => e.IsActive)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.FullName)
            .Select(e => new EmployeeDto(
                e.Id,
                e.FullName,
                e.Email,
                e.JobTitle,
                e.Role,
                e.Department.Name,
                e.HireDate,
                e.BaseSalary,
                e.PhotoData != null))
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeDetailDto?> GetEmployeeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EmployeeDetailDto(
                e.Id,
                e.FullName,
                e.SocialName,
                e.Cpf,
                e.Rg,
                e.BirthDate,
                e.Gender,
                e.Email,
                e.Phone,
                e.MobilePhone,
                e.JobTitle,
                e.Role,
                e.DepartmentId,
                e.Department.Name,
                e.HireDate,
                e.AddressStreet,
                e.AddressNumber,
                e.AddressComplement,
                e.AddressNeighborhood,
                e.AddressCity,
                e.AddressState,
                e.AddressZipCode,
                e.EmergencyContactName,
                e.EmergencyContactPhone,
                e.Notes,
                e.PhotoData,
                e.BaseSalary,
                e.IsActive,
                e.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<EmployeeDetailDto> CreateEmployeeAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = MapToEntity(new Employee(), request);
        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetEmployeeByIdAsync(employee.Id, cancellationToken))!;
    }

    public async Task<EmployeeDetailDto?> UpdateEmployeeAsync(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        employee.FullName = request.FullName.Trim();
        employee.SocialName = request.SocialName?.Trim();
        employee.Cpf = string.IsNullOrWhiteSpace(request.Cpf) ? null : FieldNormalizers.NormalizeDigits(request.Cpf);
        employee.Rg = request.Rg?.Trim();
        employee.BirthDate = request.BirthDate;
        employee.Gender = request.Gender;
        employee.Email = request.Email?.Trim();
        employee.Phone = request.Phone?.Trim();
        employee.MobilePhone = request.MobilePhone?.Trim();
        employee.JobTitle = request.JobTitle?.Trim();
        employee.Role = request.Role;
        employee.DepartmentId = request.DepartmentId;
        employee.HireDate = request.HireDate;
        employee.AddressStreet = request.AddressStreet?.Trim();
        employee.AddressNumber = request.AddressNumber?.Trim();
        employee.AddressComplement = request.AddressComplement?.Trim();
        employee.AddressNeighborhood = request.AddressNeighborhood?.Trim();
        employee.AddressCity = request.AddressCity?.Trim();
        employee.AddressState = request.AddressState?.Trim()?.ToUpperInvariant();
        employee.AddressZipCode = FieldNormalizers.NormalizeZipCode(request.AddressZipCode);
        employee.EmergencyContactName = request.EmergencyContactName?.Trim();
        employee.EmergencyContactPhone = request.EmergencyContactPhone?.Trim();
        employee.Notes = request.Notes?.Trim();
        employee.BaseSalary = request.BaseSalary;
        employee.IsActive = request.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;

        if (request.PhotoData is not null)
        {
            employee.PhotoData = NormalizePhoto(request.PhotoData);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetEmployeeByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeShiftDto>> GetShiftsAsync(
        DateOnly? date,
        DateOnly? from,
        DateOnly? to,
        ShiftType? shiftType,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.EmployeeShifts.AsNoTracking().Where(s => s.IsActive);

        if (date.HasValue)
        {
            query = query.Where(s => s.ShiftDate == date.Value);
        }
        else
        {
            if (from.HasValue)
            {
                query = query.Where(s => s.ShiftDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(s => s.ShiftDate <= to.Value);
            }
        }

        if (shiftType.HasValue)
        {
            query = query.Where(s => s.ShiftType == shiftType.Value);
        }

        return await query
            .OrderBy(s => s.ShiftDate)
            .ThenBy(s => s.ShiftType)
            .Select(s => new EmployeeShiftDto(
                s.Id,
                s.EmployeeId,
                s.Employee.FullName,
                s.Department.Name,
                s.ShiftDate,
                s.ShiftType))
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeShiftDto> CreateShiftAsync(
        CreateShiftRequest request,
        CancellationToken cancellationToken = default)
    {
        var duplicate = await dbContext.EmployeeShifts
            .AnyAsync(s => s.IsActive
                && s.EmployeeId == request.EmployeeId
                && s.ShiftDate == request.ShiftDate
                && s.ShiftType == request.ShiftType, cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("Já existe escala para este colaborador na mesma data e turno.");
        }

        var shift = new EmployeeShift
        {
            EmployeeId = request.EmployeeId,
            DepartmentId = request.DepartmentId,
            ShiftDate = request.ShiftDate,
            ShiftType = request.ShiftType
        };

        dbContext.EmployeeShifts.Add(shift);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await dbContext.EmployeeShifts
            .AsNoTracking()
            .Where(s => s.Id == shift.Id)
            .Select(s => new EmployeeShiftDto(
                s.Id,
                s.EmployeeId,
                s.Employee.FullName,
                s.Department.Name,
                s.ShiftDate,
                s.ShiftType))
            .FirstAsync(cancellationToken));
    }

    public async Task<bool> DeleteShiftAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shift = await dbContext.EmployeeShifts.FirstOrDefaultAsync(s => s.Id == id && s.IsActive, cancellationToken);
        if (shift is null)
        {
            return false;
        }

        shift.IsActive = false;
        shift.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<EmployeeHrEventDto>> GetHrEventsAsync(
        HrEventType? eventType,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.EmployeeHrEvents.AsNoTracking().Where(e => e.IsActive);

        if (eventType.HasValue)
        {
            query = query.Where(e => e.EventType == eventType.Value);
        }

        return await query
            .OrderByDescending(e => e.StartDate)
            .ThenBy(e => e.Employee.FullName)
            .Select(e => new EmployeeHrEventDto(
                e.Id,
                e.EmployeeId,
                e.Employee.FullName,
                e.EventType,
                e.Title,
                e.Detail,
                e.StartDate,
                e.EndDate,
                e.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeHrEventDto> CreateHrEventAsync(
        CreateEmployeeHrEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var employeeExists = await dbContext.Employees
            .AnyAsync(e => e.Id == request.EmployeeId && e.IsActive, cancellationToken);

        if (!employeeExists)
        {
            throw new InvalidOperationException("Colaborador não encontrado.");
        }

        var hrEvent = new EmployeeHrEvent
        {
            EmployeeId = request.EmployeeId,
            EventType = request.EventType,
            Title = request.Title.Trim(),
            Detail = request.Detail.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Notes = request.Notes?.Trim(),
        };

        dbContext.EmployeeHrEvents.Add(hrEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await dbContext.EmployeeHrEvents
            .AsNoTracking()
            .Where(e => e.Id == hrEvent.Id)
            .Select(e => new EmployeeHrEventDto(
                e.Id,
                e.EmployeeId,
                e.Employee.FullName,
                e.EventType,
                e.Title,
                e.Detail,
                e.StartDate,
                e.EndDate,
                e.Notes))
            .FirstAsync(cancellationToken));
    }

    private static Employee MapToEntity(Employee employee, CreateEmployeeRequest request)
    {
        employee.FullName = request.FullName.Trim();
        employee.SocialName = request.SocialName?.Trim();
        employee.Cpf = string.IsNullOrWhiteSpace(request.Cpf) ? null : FieldNormalizers.NormalizeDigits(request.Cpf);
        employee.Rg = request.Rg?.Trim();
        employee.BirthDate = request.BirthDate;
        employee.Gender = request.Gender;
        employee.Email = request.Email?.Trim();
        employee.Phone = request.Phone?.Trim();
        employee.MobilePhone = request.MobilePhone?.Trim();
        employee.JobTitle = request.JobTitle?.Trim();
        employee.Role = request.Role;
        employee.DepartmentId = request.DepartmentId;
        employee.HireDate = request.HireDate;
        employee.AddressStreet = request.AddressStreet?.Trim();
        employee.AddressNumber = request.AddressNumber?.Trim();
        employee.AddressComplement = request.AddressComplement?.Trim();
        employee.AddressNeighborhood = request.AddressNeighborhood?.Trim();
        employee.AddressCity = request.AddressCity?.Trim();
        employee.AddressState = request.AddressState?.Trim()?.ToUpperInvariant();
        employee.AddressZipCode = FieldNormalizers.NormalizeZipCode(request.AddressZipCode);
        employee.EmergencyContactName = request.EmergencyContactName?.Trim();
        employee.EmergencyContactPhone = request.EmergencyContactPhone?.Trim();
        employee.Notes = request.Notes?.Trim();
        employee.BaseSalary = request.BaseSalary;
        employee.PhotoData = NormalizePhoto(request.PhotoData);
        return employee;
    }

    private static (DateOnly WeekStart, DateOnly WeekEnd) GetWeekRange(DateOnly date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var mondayOffset = dayOfWeek == 0 ? -6 : 1 - dayOfWeek;
        var weekStart = date.AddDays(mondayOffset);
        return (weekStart, weekStart.AddDays(6));
    }

    private static string? NormalizePhoto(string? photoData)
    {
        if (string.IsNullOrWhiteSpace(photoData))
        {
            return null;
        }

        if (photoData.Length > 500_000)
        {
            throw new InvalidOperationException("A foto excede o tamanho máximo permitido.");
        }

        return photoData;
    }
}
