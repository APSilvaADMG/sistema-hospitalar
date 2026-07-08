using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Services.Payroll;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Dados de demonstração de RH: departamentos, colaboradores, escalas, folha e eventos.
/// Idempotente — não duplica se já houver colaboradores demo suficientes.
/// </summary>
public static class HrDemoSeed
{
    public const string DemoMarker = "gth-hr-demo-v1";

    private static readonly string[] FirstNames =
    [
        "Ana", "Beatriz", "Camila", "Daniela", "Eduardo", "Fernanda", "Gabriel", "Helena",
        "Igor", "Juliana", "Karina", "Lucas", "Mariana", "Natalia", "Otávio", "Patrícia",
        "Rafael", "Sandra", "Thiago", "Vanessa", "Wagner", "Yasmin", "André", "Bruna",
        "Carlos", "Débora", "Eliane", "Felipe", "Gustavo", "Isabela", "João", "Larissa",
        "Marcos", "Paula", "Renato", "Simone", "Vitor", "Amanda", "Bruno", "Cláudia",
    ];

    private static readonly string[] LastNames =
    [
        "Silva", "Santos", "Oliveira", "Souza", "Lima", "Pereira", "Costa", "Ferreira",
        "Rodrigues", "Almeida", "Nascimento", "Araújo", "Ribeiro", "Carvalho", "Gomes",
        "Martins", "Rocha", "Barbosa", "Dias", "Cavalcanti", "Mendes", "Freitas", "Cardoso",
        "Teixeira", "Monteiro", "Correia", "Moura", "Campos", "Nunes", "Machado",
    ];

    private static readonly (string Name, string Description)[] DepartmentCatalog =
    [
        ("Enfermagem", "Equipe de enfermagem assistencial"),
        ("UTI", "Unidade de terapia intensiva"),
        ("Pronto-Socorro", "Atendimento de urgência e emergência"),
        ("Centro Cirúrgico", "Bloco operatório"),
        ("Farmácia", "Farmácia clínica e dispensação"),
        ("Laboratório", "Análises clínicas"),
        ("Diagnóstico por Imagem", "Radiologia e tomografia"),
        ("Hotelaria", "Higienização e conforto"),
        ("Recursos Humanos", "Gestão de pessoas"),
        ("Financeiro", "Contas a pagar e receber"),
        ("Almoxarifado", "Suprimentos e materiais"),
        ("CCIH", "Controle de infecção hospitalar"),
    ];

    public static async Task EnsureAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var markerCount = await db.Employees
            .CountAsync(e => e.Notes != null && e.Notes.Contains(DemoMarker), cancellationToken);

        var departments = await EnsureDepartmentsAsync(db, cancellationToken);
        List<Employee> employees;

        if (markerCount >= 50)
        {
            employees = await db.Employees
                .Where(e => e.IsActive && e.Notes != null && e.Notes.Contains(DemoMarker))
                .ToListAsync(cancellationToken);
        }
        else
        {
            logger.LogInformation("Aplicando dados de demonstração de RH...");
            employees = await SeedEmployeesAsync(db, departments, cancellationToken);
            await SeedHrEventsAsync(db, employees, cancellationToken);
        }

        await EnsureEmployeeSalariesForAllActiveAsync(db, cancellationToken);
        await SeedShiftsAsync(db, employees, departments, cancellationToken);
        await SeedPayrollAsync(db, employees, cancellationToken);

        logger.LogInformation(
            "RH demo: {EmployeeCount} colaboradores, {DeptCount} departamentos.",
            employees.Count,
            departments.Count);
    }

    private static async Task<Dictionary<string, Department>> EnsureDepartmentsAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var existing = await db.Departments.ToListAsync(cancellationToken);
        var map = existing.ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var (name, description) in DepartmentCatalog)
        {
            if (map.ContainsKey(name))
            {
                continue;
            }

            var dept = new Department { Name = name, Description = description };
            db.Departments.Add(dept);
            map[name] = dept;
        }

        await db.SaveChangesAsync(cancellationToken);
        return map;
    }

    private static async Task<List<Employee>> SeedEmployeesAsync(
        AppDbContext db,
        IReadOnlyDictionary<string, Department> departments,
        CancellationToken cancellationToken)
    {
        var targetCount = 82;
        var existingDemo = await db.Employees
            .Where(e => e.Notes != null && e.Notes.Contains(DemoMarker))
            .ToListAsync(cancellationToken);

        if (existingDemo.Count >= targetCount)
        {
            return existingDemo;
        }

        var rnd = new Random(20260706);
        var deptList = departments.Values.ToList();
        var jobProfiles = BuildJobProfiles();
        var toCreate = targetCount - existingDemo.Count;
        var created = new List<Employee>();

        for (var i = 0; i < toCreate; i++)
        {
            var profile = jobProfiles[rnd.Next(jobProfiles.Count)];
            var dept = departments.TryGetValue(profile.DeptName, out var d) ? d : deptList[rnd.Next(deptList.Count)];
            var first = FirstNames[rnd.Next(FirstNames.Length)];
            var last = LastNames[rnd.Next(LastNames.Length)];
            var second = rnd.Next(3) == 0 ? $" {LastNames[rnd.Next(LastNames.Length)]}" : string.Empty;
            var fullName = $"{first} {last}{second}";
            var slug = $"{first}.{last}".ToLowerInvariant().Replace(' ', '.');
            var hireYear = rnd.Next(2016, 2026);
            var hireMonth = rnd.Next(1, 13);
            var hireDay = rnd.Next(1, 28);

            var employee = new Employee
            {
                FullName = fullName,
                Role = profile.Role,
                Department = dept,
                JobTitle = profile.JobTitle,
                BaseSalary = PayrollCalculationService.DefaultSalaryForRole(profile.Role) + rnd.Next(-300, 501),
                Email = $"{slug}{existingDemo.Count + i + 1}@hospital.local",
                HireDate = new DateOnly(hireYear, hireMonth, hireDay),
                BirthDate = new DateOnly(rnd.Next(1975, 2001), rnd.Next(1, 13), rnd.Next(1, 28)),
                Gender = rnd.Next(2) == 0 ? Gender.Female : Gender.Male,
                Phone = $"(11) 3{rnd.Next(100, 999):000}-{rnd.Next(1000, 9999):0000}",
                MobilePhone = $"(11) 9{rnd.Next(1000, 9999):0000}-{rnd.Next(1000, 9999):0000}",
                Notes = $"Colaborador demo. {DemoMarker}",
            };

            db.Employees.Add(employee);
            created.Add(employee);
        }

        await db.SaveChangesAsync(cancellationToken);
        existingDemo.AddRange(created);
        await EnsureEmployeeSalariesAsync(db, existingDemo, cancellationToken);
        return existingDemo;
    }

    private static async Task EnsureEmployeeSalariesAsync(
        AppDbContext db,
        IReadOnlyList<Employee> employees,
        CancellationToken cancellationToken)
    {
        await EnsureEmployeeSalariesForAllActiveAsync(db, cancellationToken);
    }

    private static async Task EnsureEmployeeSalariesForAllActiveAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var rnd = new Random(20260710);
        var employees = await db.Employees
            .Where(e => e.IsActive && e.BaseSalary <= 0)
            .ToListAsync(cancellationToken);

        if (employees.Count == 0)
        {
            return;
        }

        foreach (var employee in employees)
        {
            employee.BaseSalary = PayrollCalculationService.DefaultSalaryForRole(employee.Role) + rnd.Next(-300, 501);
            employee.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedShiftsAsync(
        AppDbContext db,
        IReadOnlyList<Employee> employees,
        IReadOnlyDictionary<string, Department> departments,
        CancellationToken cancellationToken)
    {
        if (employees.Count == 0)
        {
            return;
        }

        var hasDemoShifts = await db.EmployeeShifts
            .AnyAsync(s => s.Employee.Notes != null && s.Employee.Notes.Contains(DemoMarker), cancellationToken);

        if (hasDemoShifts)
        {
            return;
        }

        var rnd = new Random(20260707);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-14);
        var to = today.AddDays(7);
        var shiftTypes = new[] { ShiftType.Morning, ShiftType.Afternoon, ShiftType.Night };
        var deptList = departments.Values.ToList();

        var usedKeys = (await db.EmployeeShifts
            .AsNoTracking()
            .Where(s => s.ShiftDate >= from && s.ShiftDate <= to)
            .Select(s => new { s.EmployeeId, s.ShiftDate, s.ShiftType })
            .ToListAsync(cancellationToken))
            .Select(s => (s.EmployeeId, s.ShiftDate, s.ShiftType))
            .ToHashSet();

        var pending = new List<EmployeeShift>();

        for (var dayOffset = -14; dayOffset <= 7; dayOffset++)
        {
            var shiftDate = today.AddDays(dayOffset);
            var shiftsToday = rnd.Next(8, 16);

            for (var s = 0; s < shiftsToday; s++)
            {
                var employee = employees[rnd.Next(employees.Count)];
                var shiftType = shiftTypes[rnd.Next(shiftTypes.Length)];
                var dept = deptList[rnd.Next(deptList.Count)];

                if (!usedKeys.Add((employee.Id, shiftDate, shiftType)))
                {
                    continue;
                }

                pending.Add(new EmployeeShift
                {
                    EmployeeId = employee.Id,
                    DepartmentId = dept.Id,
                    ShiftDate = shiftDate,
                    ShiftType = shiftType,
                });
            }
        }

        if (pending.Count == 0)
        {
            return;
        }

        db.EmployeeShifts.AddRange(pending);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedPayrollAsync(
        AppDbContext db,
        IReadOnlyList<Employee> _,
        CancellationToken cancellationToken)
    {
        var employees = await PayrollSeedHelper.GetActiveEmployeesAsync(db, cancellationToken);
        if (employees.Count == 0)
        {
            return;
        }

        var existingRuns = await db.PayrollRuns
            .CountAsync(r => r.Notes != null && r.Notes.Contains(DemoMarker), cancellationToken);

        if (existingRuns >= 6)
        {
            return;
        }

        var rnd = new Random(20260708);
        var now = DateTime.UtcNow;

        for (var m = 1; m <= 6; m++)
        {
            var refDate = DateOnly.FromDateTime(now.AddMonths(-m));

            if (await db.PayrollRuns.AnyAsync(
                    r => r.Notes != null && r.Notes.Contains(DemoMarker)
                        && r.Year == refDate.Year && r.Month == refDate.Month,
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
                DemoMarker,
                refDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(25),
                refDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(28),
                isPaid ? refDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(30) : null));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedHrEventsAsync(
        AppDbContext db,
        IReadOnlyList<Employee> employees,
        CancellationToken cancellationToken)
    {
        if (employees.Count == 0)
        {
            return;
        }

        if (await db.EmployeeHrEvents.AnyAsync(e => e.Notes != null && e.Notes.Contains(DemoMarker), cancellationToken))
        {
            return;
        }

        var rnd = new Random(20260709);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var events = new List<EmployeeHrEvent>();

        var vacationTitles = new[] { "Férias agendadas", "Férias coletivas", "Recesso de fim de ano" };
        var trainingTitles = new[]
        {
            "NR-32 — Segurança em serviços de saúde",
            "ACLS — Suporte avançado de vida",
            "BLS — Suporte básico de vida",
            "Curso de biossegurança",
            "Treinamento CCIH — Higienização das mãos",
        };
        var reviewTitles = new[] { "Avaliação semestral", "Avaliação de desempenho", "Feedback 360°" };

        for (var i = 0; i < 14; i++)
        {
            var employee = employees[rnd.Next(employees.Count)];
            var start = today.AddDays(rnd.Next(15, 90));
            events.Add(new EmployeeHrEvent
            {
                EmployeeId = employee.Id,
                EventType = HrEventType.Vacation,
                Title = vacationTitles[rnd.Next(vacationTitles.Length)],
                Detail = $"{rnd.Next(10, 31)} dias — período aquisitivo",
                StartDate = start,
                EndDate = start.AddDays(rnd.Next(10, 31)),
                Notes = DemoMarker,
            });
        }

        for (var i = 0; i < 14; i++)
        {
            var employee = employees[rnd.Next(employees.Count)];
            var start = today.AddDays(-rnd.Next(1, 120));
            events.Add(new EmployeeHrEvent
            {
                EmployeeId = employee.Id,
                EventType = HrEventType.Training,
                Title = trainingTitles[rnd.Next(trainingTitles.Length)],
                Detail = $"Carga horária: {rnd.Next(4, 17)}h — certificado emitido",
                StartDate = start,
                EndDate = start,
                Notes = DemoMarker,
            });
        }

        for (var i = 0; i < 12; i++)
        {
            var employee = employees[rnd.Next(employees.Count)];
            var start = today.AddDays(-rnd.Next(30, 180));
            events.Add(new EmployeeHrEvent
            {
                EmployeeId = employee.Id,
                EventType = HrEventType.PerformanceReview,
                Title = reviewTitles[rnd.Next(reviewTitles.Length)],
                Detail = $"Nota geral: {rnd.Next(70, 96)}/100 — metas do semestre",
                StartDate = start,
                Notes = DemoMarker,
            });
        }

        db.EmployeeHrEvents.AddRange(events);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<(string DeptName, string JobTitle, EmployeeRole Role)> BuildJobProfiles() =>
    [
        ("Enfermagem", "Enfermeiro(a)", EmployeeRole.Nurse),
        ("Enfermagem", "Técnico(a) de enfermagem", EmployeeRole.Technician),
        ("UTI", "Enfermeiro(a) UTI", EmployeeRole.Nurse),
        ("UTI", "Técnico(a) de enfermagem UTI", EmployeeRole.Technician),
        ("Pronto-Socorro", "Enfermeiro(a) PS", EmployeeRole.Nurse),
        ("Pronto-Socorro", "Auxiliar de enfermagem", EmployeeRole.Technician),
        ("Centro Cirúrgico", "Instrumentador(a)", EmployeeRole.Technician),
        ("Centro Cirúrgico", "Enfermeiro(a) circulante", EmployeeRole.Nurse),
        ("Farmácia", "Farmacêutico(a)", EmployeeRole.Technician),
        ("Farmácia", "Auxiliar de farmácia", EmployeeRole.Administrative),
        ("Laboratório", "Biomédico(a)", EmployeeRole.Technician),
        ("Laboratório", "Técnico(a) de laboratório", EmployeeRole.Technician),
        ("Diagnóstico por Imagem", "Técnico(a) em radiologia", EmployeeRole.Technician),
        ("Hotelaria", "Supervisor(a) de hotelaria", EmployeeRole.Administrative),
        ("Hotelaria", "Camareira(o)", EmployeeRole.Other),
        ("Recursos Humanos", "Analista de RH", EmployeeRole.Administrative),
        ("Financeiro", "Analista financeiro", EmployeeRole.Administrative),
        ("Financeiro", "Gerente financeiro", EmployeeRole.Manager),
        ("Almoxarifado", "Almoxarife", EmployeeRole.Administrative),
        ("CCIH", "Enfermeiro(a) epidemiologista", EmployeeRole.Nurse),
        ("UTI", "Coordenador(a) de UTI", EmployeeRole.Manager),
        ("Pronto-Socorro", "Coordenador(a) médico PS", EmployeeRole.Manager),
        ("Enfermagem", "Supervisor(a) de enfermagem", EmployeeRole.Manager),
        ("Financeiro", "Assistente administrativo", EmployeeRole.Administrative),
    ];
}
