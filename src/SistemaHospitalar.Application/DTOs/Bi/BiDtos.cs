namespace SistemaHospitalar.Application.DTOs.Bi;

public record BiDashboardDto(
    int TotalPatients,
    int ActiveHospitalizations,
    int AppointmentsToday,
    int SurgeriesToday,
    int LabOrdersPending,
    int ImagingStudiesPending,
    decimal RevenueThisMonth,
    decimal RevenueLastMonth,
    decimal RevenueGrowthPercent,
    decimal RevenuePending,
    decimal BedOccupancyRate,
    int OccupiedBeds,
    int TotalBeds,
    int EmergencyWaiting,
    int EmergencyInCare,
    int FinancialAccountsOpen,
    int LowStockProducts,
    int PurchaseOrdersPending,
    int TissGuidesPending,
    decimal TissAmountPending,
    IReadOnlyList<BiMonthlyStatDto> MonthlyAppointments,
    IReadOnlyList<BiMonthlyStatDto> MonthlyRevenue,
    IReadOnlyList<BiMonthlyStatDto> MonthlyExpenses,
    IReadOnlyList<BiMonthlyStatDto> MonthlyHospitalizations,
    decimal AverageLengthOfStayDays,
    int DischargesThisMonth,
    decimal BedTurnoverRate,
    decimal MonthlyBedTurnover,
    decimal ExpenseThisMonth,
    decimal ExpenseLastMonth,
    decimal ExpenseGrowthPercent,
    decimal OverdueReceivable,
    int OverdueReceivableCount,
    decimal DefaultRatePercent,
    int MedicalProductionThisMonth,
    int HospitalProductionThisMonth,
    IReadOnlyList<BiCategoryStatDto> RevenueByCategory,
    IReadOnlyList<BiStatusCountDto> TissGuidesByStatus,
    IReadOnlyList<BiStatusCountDto> LabOrdersByStatus,
    IReadOnlyList<BiStatusCountDto> FinancialAccountsByStatus,
    IReadOnlyList<BiStatusCountDto> ImagingByStatus,
    IReadOnlyList<BiStatusCountDto> EmergencyByUrgency,
    IReadOnlyList<BiWardOccupancyDto> WardOccupancy,
    IReadOnlyList<BiSpecialtyStatDto> TopSpecialties,
    IReadOnlyList<BiLowStockItemDto> LowStockItems,
    DateTime GeneratedAt);

public record BiMonthlyStatDto(string Label, int Count, decimal? Amount = null);

public record BiStatusCountDto(string Label, int Count, decimal? Amount);

public record BiCategoryStatDto(string Label, decimal Amount, int Count);

public record BiWardOccupancyDto(string WardName, int TotalBeds, int OccupiedBeds, decimal OccupancyRate);

public record BiSpecialtyStatDto(string SpecialtyName, int AppointmentsThisMonth);

public record BiLowStockItemDto(string ProductName, string Sku, decimal OnHand, decimal Minimum, string Unit);
