using SistemaHospitalar.Application.DTOs.Bi;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Dashboard;

public record OperationalDashboardDto(
    int TotalPatients,
    int AppointmentsToday,
    int AppointmentsPendingToday,
    int ActiveHospitalizations,
    int SurgeriesToday,
    int OccupiedBeds,
    int TotalBeds,
    decimal BedOccupancyRate,
    int EmergencyWaiting,
    int EmergencyInCare,
    int EmergencyCritical,
    int TriageToday,
    int TriageEmergencyToday,
    int LabOrdersPending,
    int ImagingStudiesPending,
    decimal RevenueThisMonth,
    decimal RevenuePending,
    int FinancialAccountsOpen,
    decimal PayablePending,
    decimal ExpenseThisMonth,
    int PayableAccountsOpen,
    decimal OverdueReceivable,
    decimal OverduePayable,
    int LowStockProducts,
    int ParkingOccupied,
    int ParkingAwaitingPayment,
    int VisitorsInside,
    int OpenSecurityIncidents,
    int UnreadNotifications,
    IReadOnlyList<DashboardAppointmentItemDto> AppointmentsTodayList,
    IReadOnlyList<DashboardFinancialMonthlyPointDto> RevenueExpenseMonthly,
    IReadOnlyList<DashboardWeeklyCalendarItemDto> WeeklyCalendar,
    IReadOnlyList<DashboardDepartmentRevenueDto> DepartmentRevenue,
    IReadOnlyList<DashboardEmergencyItemDto> EmergencyQueue,
    IReadOnlyList<BiMonthlyStatDto> MonthlyAppointments,
    IReadOnlyList<BiStatusCountDto> LabOrdersByStatus,
    IReadOnlyList<DashboardBirthdayEmployeeDto> MonthBirthdays,
    decimal RevenueToday,
    int AvailableBeds,
    int CleaningBeds,
    int MaintenanceBeds,
    int AttendancesToday,
    double AverageEmergencyWaitMinutes,
    int EmergencySlaViolations,
    int IntegrationFailures,
    IReadOnlyList<DashboardHourlyStatDto> HourlyAttendances,
    IReadOnlyList<BiStatusCountDto> ProductionBySpecialty,
    IReadOnlyList<DashboardAlertDto> Alerts,
    IReadOnlyList<BiStatusCountDto> AppointmentStatusBreakdown,
    DateTime GeneratedAt);

public record DashboardFinancialMonthlyPointDto(string MonthLabel, decimal Revenue, decimal Expense);

public record DashboardWeeklyCalendarItemDto(
    Guid AppointmentId,
    DateTime ScheduledAt,
    string PatientName,
    string ProfessionalName,
    string SpecialtyName);

public record DashboardDepartmentRevenueDto(string DepartmentCode, string DepartmentLabel, decimal Amount);

public record DashboardHourlyStatDto(int Hour, int Count);

public record DashboardAlertDto(
    string Code,
    string Severity,
    string Title,
    string Message,
    string? LinkPath);

public record DashboardBirthdayEmployeeDto(
    Guid Id,
    string FullName,
    DateOnly BirthDate,
    string? PhotoData,
    string? JobTitle,
    string DepartmentName);

public record DashboardAppointmentItemDto(
    Guid Id,
    DateTime ScheduledAt,
    string PatientName,
    string ProfessionalName,
    string SpecialtyName,
    AppointmentStatus Status);

public record DashboardEmergencyItemDto(
    Guid Id,
    string PatientName,
    string ChiefComplaint,
    TriageUrgency Urgency,
    EmergencyVisitStatus Status,
    DateTime ArrivedAt);
