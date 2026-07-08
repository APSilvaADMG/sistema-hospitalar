using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Services.Payroll;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Carga operacional idempotente: RH, sala de espera, estoque auxiliar e contas a pagar coerentes.
/// </summary>
public static class OperationalDemoSeed
{
    public const string Marker = "gth-operational-demo-v1";
    public const string EmployeeEmailDomain = "@demo.hospital.local";

    public static async Task EnsureAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await EnsureDepartmentsAsync(db, cancellationToken);
        var employeesCreated = await EnsureEmployeesAsync(db, cancellationToken);
        await EnsureShiftsAsync(db, cancellationToken);
        await EnsurePayrollAsync(db, cancellationToken);
        await EnsureHrEventsAsync(db, cancellationToken);
        await EnsureInventoryLookupsAsync(db, cancellationToken);
        await EnsurePayableAccountsAsync(db, cancellationToken);
        await WarehouseDemoSeed.EnsureAsync(db, logger, cancellationToken);

        if (employeesCreated > 0)
        {
            logger.LogInformation(
                "OperationalDemoSeed: +{Employees} colaboradores; dados RH/estoque/sala de espera atualizados.",
                employeesCreated);
        }
    }

    private static async Task EnsureDepartmentsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var names = new[]
        {
            "Enfermagem", "UTI", "Pronto-Socorro", "Centro Cirúrgico", "Farmácia", "Laboratório",
            "Diagnóstico por Imagem", "Hotelaria", "Recursos Humanos", "Financeiro", "Almoxarifado",
            "CCIH", "Administrativo", "Nutrição", "Fisioterapia",
        };

        var existing = await db.Departments.AsNoTracking().Select(d => d.Name).ToListAsync(cancellationToken);
        var missing = names.Where(n => !existing.Contains(n)).Select(n => new Department { Name = n, Description = $"Setor {n}" }).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        db.Departments.AddRange(missing);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<int> EnsureEmployeesAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var demoCount = await db.Employees.CountAsync(
            e => e.IsActive && e.Email != null && e.Email.EndsWith(EmployeeEmailDomain),
            cancellationToken);

        if (demoCount >= 70)
        {
            return 0;
        }

        var departments = await db.Departments.AsNoTracking().Where(d => d.IsActive).ToListAsync(cancellationToken);
        if (departments.Count == 0)
        {
            return 0;
        }

        var firstNames = new[]
        {
            "Ana", "Bruno", "Carla", "Diego", "Elena", "Fabio", "Gabriela", "Henrique", "Isabela", "Juliano",
            "Karina", "Lucas", "Mariana", "Nicolas", "Olivia", "Paulo", "Renata", "Samuel", "Tatiana", "Vitor",
            "Amanda", "Bernardo", "Camila", "Daniel", "Elisa", "Felipe", "Giovana", "Hugo", "Ingrid", "João",
            "Larissa", "Marcos", "Natalia", "Otavio", "Patricia", "Rafael", "Simone", "Thiago", "Vanessa", "Wesley",
            "Aline", "Caio", "Debora", "Eduardo", "Fernanda", "Gustavo", "Helena", "Igor", "Julia", "Leandro",
            "Monica", "Nelson", "Priscila", "Rodrigo", "Sabrina", "Tiago", "Ursula", "Vinicius", "Yasmin", "Zeca",
            "Adriana", "Cesar", "Denise", "Emerson", "Flavia", "Guilherme", "Heloisa", "Ivan", "Janaina", "Kelly",
        };

        var lastNames = new[]
        {
            "Almeida", "Barbosa", "Cardoso", "Dias", "Esteves", "Ferreira", "Gomes", "Henrique", "Ibrahim", "Jesus",
            "Klein", "Lima", "Mendes", "Nogueira", "Oliveira", "Pereira", "Queiroz", "Ribeiro", "Souza", "Teixeira",
        };

        var titles = new (EmployeeRole Role, string Title, string Dept)[]
        {
            (EmployeeRole.Nurse, "Enfermeiro(a)", "Enfermagem"),
            (EmployeeRole.Nurse, "Enfermeiro(a) UTI", "UTI"),
            (EmployeeRole.Technician, "Técnico(a) de Enfermagem", "Pronto-Socorro"),
            (EmployeeRole.Technician, "Instrumentador(a)", "Centro Cirúrgico"),
            (EmployeeRole.Technician, "Farmacêutico(a)", "Farmácia"),
            (EmployeeRole.Technician, "Biomédico(a)", "Laboratório"),
            (EmployeeRole.Technician, "Técnico(a) de Imagem", "Diagnóstico por Imagem"),
            (EmployeeRole.Administrative, "Recepcionista", "Administrativo"),
            (EmployeeRole.Administrative, "Auxiliar administrativo", "Financeiro"),
            (EmployeeRole.Manager, "Coordenador(a)", "Almoxarifado"),
            (EmployeeRole.Other, "Nutricionista", "Nutrição"),
            (EmployeeRole.Other, "Fisioterapeuta", "Fisioterapia"),
        };

        var rnd = new Random(20260706);
        var toCreate = 75 - demoCount;
        var employees = new List<Employee>();

        for (var i = 0; i < toCreate; i++)
        {
            var spec = titles[i % titles.Length];
            var dept = departments.FirstOrDefault(d => d.Name == spec.Dept) ?? departments[i % departments.Count];
            var fn = firstNames[(i + demoCount) % firstNames.Length];
            var ln = lastNames[(i * 3 + demoCount) % lastNames.Length];
            var slug = $"{fn}.{ln}".ToLowerInvariant().Replace(" ", "");
            var hireYear = 2018 + rnd.Next(0, 7);

            employees.Add(new Employee
            {
                FullName = $"{fn} {ln}",
                Email = $"{slug}{i + demoCount}{EmployeeEmailDomain}",
                JobTitle = spec.Title,
                Role = spec.Role,
                BaseSalary = PayrollCalculationService.DefaultSalaryForRole(spec.Role) + rnd.Next(-200, 401),
                DepartmentId = dept.Id,
                HireDate = new DateOnly(hireYear, rnd.Next(1, 13), rnd.Next(1, 28)),
                BirthDate = new DateOnly(1975 + rnd.Next(0, 25), rnd.Next(1, 13), rnd.Next(1, 28)),
                MobilePhone = $"11{900000000 + rnd.Next(0, 99999999):D8}",
                Notes = Marker,
            });
        }

        db.Employees.AddRange(employees);
        await db.SaveChangesAsync(cancellationToken);
        return employees.Count;
    }

    private static async Task EnsureShiftsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.EmployeeShifts.CountAsync(cancellationToken) >= 150)
        {
            return;
        }

        var employees = await db.Employees.AsNoTracking()
            .Where(e => e.IsActive)
            .Select(e => new { e.Id, e.DepartmentId })
            .ToListAsync(cancellationToken);

        if (employees.Count == 0)
        {
            return;
        }

        var rnd = new Random(42);
        var today = DateOnly.FromDateTime(BrazilToday());
        var from = today.AddDays(-14);
        var to = today.AddDays(7);

        var existingKeys = (await db.EmployeeShifts
            .AsNoTracking()
            .Where(s => s.ShiftDate >= from && s.ShiftDate <= to)
            .Select(s => new { s.EmployeeId, s.ShiftDate, s.ShiftType })
            .ToListAsync(cancellationToken))
            .Select(s => (s.EmployeeId, s.ShiftDate, s.ShiftType))
            .ToHashSet();

        var shifts = new List<EmployeeShift>();

        for (var day = -14; day <= 7; day++)
        {
            var date = today.AddDays(day);
            var count = rnd.Next(10, 18);
            for (var s = 0; s < count; s++)
            {
                var emp = employees[rnd.Next(employees.Count)];
                var shiftType = (ShiftType)rnd.Next(1, 4);
                if (!existingKeys.Add((emp.Id, date, shiftType)))
                {
                    continue;
                }

                shifts.Add(new EmployeeShift
                {
                    EmployeeId = emp.Id,
                    DepartmentId = emp.DepartmentId,
                    ShiftDate = date,
                    ShiftType = shiftType,
                });
            }
        }

        if (shifts.Count == 0)
        {
            return;
        }

        db.EmployeeShifts.AddRange(shifts);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsurePayrollAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var employees = await PayrollSeedHelper.GetActiveEmployeesAsync(db, cancellationToken);
        if (employees.Count == 0)
        {
            return;
        }

        var rnd = new Random(99);
        var now = DateTime.UtcNow;

        for (var m = 1; m <= 6; m++)
        {
            var refDate = DateOnly.FromDateTime(now.AddMonths(-m));

            if (await db.PayrollRuns.AnyAsync(
                    r => r.IsActive
                        && r.Notes != null
                        && r.Notes == Marker
                        && r.Year == refDate.Year
                        && r.Month == refDate.Month,
                    cancellationToken))
            {
                continue;
            }
            var periodStart = new DateOnly(refDate.Year, refDate.Month, 1);
            var periodEnd = new DateOnly(refDate.Year, refDate.Month, DateTime.DaysInMonth(refDate.Year, refDate.Month));
            var shiftStats = await PayrollSeedHelper.GetShiftStatsAsync(
                db,
                employees.Select(e => e.Id).ToList(),
                periodStart,
                periodEnd,
                cancellationToken);

            var items = employees
                .Select(employee =>
                {
                    shiftStats.TryGetValue(employee.Id, out var shifts);
                    return PayrollSeedHelper.BuildItemForEmployee(employee, refDate.Year, refDate.Month, shifts, rnd);
                })
                .ToList();

            var isPaid = m > 1;
            db.PayrollRuns.Add(PayrollSeedHelper.BuildRun(
                refDate.Year,
                refDate.Month,
                items,
                isPaid ? PayrollRunStatus.Paid : PayrollRunStatus.Approved,
                Marker,
                refDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(25),
                refDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(28),
                isPaid ? refDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(30) : null));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureHrEventsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.EmployeeHrEvents.AnyAsync(e => e.Notes != null && e.Notes == Marker, cancellationToken))
        {
            return;
        }

        var employees = await db.Employees.AsNoTracking().Where(e => e.IsActive).Take(40).ToListAsync(cancellationToken);
        if (employees.Count == 0)
        {
            return;
        }

        var rnd = new Random(7);
        var events = new List<EmployeeHrEvent>();
        var today = DateOnly.FromDateTime(BrazilToday());

        foreach (var emp in employees.Take(15))
        {
            events.Add(new EmployeeHrEvent
            {
                EmployeeId = emp.Id,
                EventType = HrEventType.Vacation,
                Title = "Férias programadas",
                Detail = $"{rnd.Next(15, 31)} dias — período aquisitivo",
                StartDate = today.AddDays(rnd.Next(10, 60)),
                EndDate = today.AddDays(rnd.Next(25, 75)),
                Notes = Marker,
            });
        }

        foreach (var emp in employees.Skip(5).Take(12))
        {
            events.Add(new EmployeeHrEvent
            {
                EmployeeId = emp.Id,
                EventType = HrEventType.Training,
                Title = rnd.NextDouble() < 0.5 ? "NR-32 — Serviços de saúde" : "ACLS — Suporte avançado de vida",
                Detail = $"Carga horária: {rnd.Next(4, 17)}h — certificado válido 2 anos",
                StartDate = today.AddDays(-rnd.Next(1, 90)),
                Notes = Marker,
            });
        }

        foreach (var emp in employees.Skip(10).Take(10))
        {
            events.Add(new EmployeeHrEvent
            {
                EmployeeId = emp.Id,
                EventType = HrEventType.PerformanceReview,
                Title = "Avaliação de desempenho semestral",
                Detail = $"Nota composta: {rnd.Next(72, 98)}/100 — metas assistenciais atingidas",
                StartDate = today.AddDays(-rnd.Next(5, 45)),
                Notes = Marker,
            });
        }

        db.EmployeeHrEvents.AddRange(events);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureInventoryLookupsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.InventoryLookupItems.AnyAsync(i => i.Name == "OPME", cancellationToken))
        {
            return;
        }

        var categories = new[] { "Medicamento", "Material Hospitalar", "OPME", "Nutrição", "Higiene", "Radiologia", "Laboratório", "Cirurgia", "UTI", "Hotelaria" };
        var locations = new[] { "Almoxarifado Central — A1", "Almoxarifado Central — B2", "Farmácia Central", "UTI — Posto 1", "Centro Cirúrgico", "PS — Maleta", "Laboratório", "Hotelaria" };
        var manufacturers = new[] { "EMS", "Baxter", "Medley", "BD", "3M", "Cremer", "Descarpack", "Neo Química", "Supermax", "Helm", "Karsten", "Roche" };

        var items = new List<InventoryLookupItem>();
        foreach (var c in categories)
        {
            items.Add(new InventoryLookupItem { Type = InventoryLookupType.Category, Name = c });
        }

        foreach (var l in locations)
        {
            items.Add(new InventoryLookupItem { Type = InventoryLookupType.Location, Name = l });
        }

        foreach (var m in manufacturers)
        {
            items.Add(new InventoryLookupItem { Type = InventoryLookupType.Manufacturer, Name = m });
        }

        db.InventoryLookupItems.AddRange(items);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsurePayableAccountsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.FinancialAccounts.AnyAsync(
                f => f.IsActive && f.Notes != null && f.Notes == Marker,
                cancellationToken))
        {
            return;
        }

        var supplier = await db.Suppliers.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var today = DateTime.UtcNow;

        var accounts = new List<FinancialAccount>
        {
            new()
            {
                Direction = FinancialAccountDirection.Payable,
                SupplierId = supplier?.Id,
                CounterpartyName = supplier?.Name ?? "MedDistribuidora Brasil",
                Category = FinancialAccountCategory.SupplierPurchase,
                Description = supplier is not null
                    ? $"NF 45821 — Material médico-hospitalar — {supplier.Name}"
                    : "NF 45821 — Material médico-hospitalar — MedDistribuidora",
                Amount = 47850.90m,
                DueDate = today.AddDays(18),
                Notes = $"{Marker}|pedido PC-2026-0142",
            },
            new()
            {
                Direction = FinancialAccountDirection.Payable,
                CounterpartyName = "Insumos Hospitalares SP",
                Category = FinancialAccountCategory.SupplierPurchase,
                Description = "NF 99231 — Luvas, gazes e seringas — pedido PC-2026-0138",
                Amount = 12340.50m,
                DueDate = today.AddDays(12),
                Notes = Marker,
            },
            new()
            {
                Direction = FinancialAccountDirection.Payable,
                CounterpartyName = "Companhia de Energia",
                Category = FinancialAccountCategory.Utilities,
                Description = "Energia elétrica — unidade hospitalar — ref. mês anterior",
                Amount = 68420.00m,
                DueDate = today.AddDays(8),
                Notes = Marker,
            },
            new()
            {
                Direction = FinancialAccountDirection.Payable,
                CounterpartyName = "Folha de pagamento",
                Category = FinancialAccountCategory.Payroll,
                Description = $"Folha {today.Month:D2}/{today.Year} — todos os colaboradores ativos",
                Amount = 892450.00m,
                DueDate = today.AddDays(5),
                Notes = Marker,
            },
            new()
            {
                Direction = FinancialAccountDirection.Payable,
                CounterpartyName = "Manutenção predial",
                Category = FinancialAccountCategory.Maintenance,
                Description = "Contrato manutenção HVAC — torre principal",
                Amount = 15800.00m,
                DueDate = today.AddDays(20),
                Notes = Marker,
            },
        };

        db.FinancialAccounts.AddRange(accounts);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static DateTime BrazilToday()
    {
        var tz = ResolveBrazilTimeZone();
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
    }

    private static DateTime BrazilLocalToUtc(DateTime localDateTime)
    {
        var tz = ResolveBrazilTimeZone();
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), tz);
    }

    private static TimeZoneInfo ResolveBrazilTimeZone()
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById("America/Sao_Paulo", out var iana))
        {
            return iana;
        }

        return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
    }
}
