using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class TransportSeed
{
    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.TransportAssets.AnyAsync(cancellationToken))
        {
            dbContext.TransportAssets.AddRange(
                new TransportAsset
                {
                    Code = "MACA-01",
                    AssetTag = "PAT-2024-001",
                    AssetType = TransportAssetType.Stretcher,
                    Sector = "Internação — Bloco B",
                    Status = TransportAssetStatus.Available,
                    TrackingCode = "QR-MACA-01",
                },
                new TransportAsset
                {
                    Code = "MACA-02",
                    AssetTag = "PAT-2024-002",
                    AssetType = TransportAssetType.Stretcher,
                    Sector = "Pronto Atendimento",
                    Status = TransportAssetStatus.Available,
                    TrackingCode = "QR-MACA-02",
                },
                new TransportAsset
                {
                    Code = "CADEIRA-01",
                    AssetTag = "PAT-2024-010",
                    AssetType = TransportAssetType.Wheelchair,
                    Sector = "Recepção",
                    Status = TransportAssetStatus.Available,
                    TrackingCode = "QR-CAD-01",
                },
                new TransportAsset
                {
                    Code = "VE-01",
                    AssetTag = "PAT-2024-020",
                    AssetType = TransportAssetType.ElectricVehicle,
                    Sector = "Central de Transportes",
                    Status = TransportAssetStatus.Available,
                    TrackingCode = "QR-VE-01",
                });
        }

        if (!await dbContext.Employees.AnyAsync(e => e.JobTitle != null && e.JobTitle.Contains("Maqueiro"), cancellationToken))
        {
            var dept = await dbContext.Departments.FirstOrDefaultAsync(cancellationToken);
            if (dept is not null)
            {
                dbContext.Employees.AddRange(
                    new Employee
                    {
                        FullName = "João Silva",
                        JobTitle = "Maqueiro",
                        Role = EmployeeRole.Other,
                        HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
                        DepartmentId = dept.Id,
                    },
                    new Employee
                    {
                        FullName = "Maria Souza",
                        JobTitle = "Maqueiro",
                        Role = EmployeeRole.Other,
                        HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                        DepartmentId = dept.Id,
                    },
                    new Employee
                    {
                        FullName = "José Santos",
                        JobTitle = "Maqueiro",
                        Role = EmployeeRole.Other,
                        HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-8)),
                        DepartmentId = dept.Id,
                    });
            }
        }

        if (!await dbContext.TransportRequests.AnyAsync(cancellationToken))
        {
            dbContext.TransportRequests.AddRange(
                new TransportRequest
                {
                    PatientName = "Ana Paula Costa",
                    OriginType = TransportLocationType.Emergency,
                    OriginDetail = "Box 12 — Pronto Atendimento",
                    DestinationType = TransportLocationType.ImagingTomography,
                    DestinationDetail = "Tomografia — Subsolo",
                    Status = TransportRequestStatus.Queued,
                    Priority = TransportPriority.Urgent,
                    RequestedAt = DateTime.UtcNow.AddMinutes(-18),
                    Notes = "Paciente com suspeita de TCE",
                },
                new TransportRequest
                {
                    PatientName = "Carlos Mendes",
                    OriginType = TransportLocationType.Hospitalization,
                    OriginDetail = "Ala B — Leito 204",
                    DestinationType = TransportLocationType.SurgeryCenter,
                    DestinationDetail = "Sala 03 — Centro Cirúrgico",
                    Status = TransportRequestStatus.InTransit,
                    Priority = TransportPriority.Normal,
                    RequestedAt = DateTime.UtcNow.AddMinutes(-42),
                    AcceptedAt = DateTime.UtcNow.AddMinutes(-35),
                    ArrivedAtOriginAt = DateTime.UtcNow.AddMinutes(-32),
                    DepartedAt = DateTime.UtcNow.AddMinutes(-28),
                },
                new TransportRequest
                {
                    PatientName = "Fernanda Lima",
                    OriginType = TransportLocationType.Icu,
                    OriginDetail = "UTI — Leito 05",
                    DestinationType = TransportLocationType.Hospitalization,
                    DestinationDetail = "Ala A — Leito 108",
                    Status = TransportRequestStatus.Completed,
                    Priority = TransportPriority.Normal,
                    RequestedAt = DateTime.UtcNow.AddHours(-2),
                    AcceptedAt = DateTime.UtcNow.AddHours(-2).AddMinutes(4),
                    ArrivedAtOriginAt = DateTime.UtcNow.AddHours(-2).AddMinutes(8),
                    DepartedAt = DateTime.UtcNow.AddHours(-2).AddMinutes(12),
                    ArrivedAtDestinationAt = DateTime.UtcNow.AddHours(-2).AddMinutes(28),
                    CompletedAt = DateTime.UtcNow.AddHours(-2).AddMinutes(30),
                });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
