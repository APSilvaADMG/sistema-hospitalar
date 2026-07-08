using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Security;
using SistemaHospitalar.Infrastructure.Services;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Gera pacientes, atendimentos, exames e registros clínicos fictícios para testes de carga.
/// Use apenas em banco de teste (GTH_ALLOW_LOAD_SEED=true).
/// </summary>
public static class HospitalLoadDataSeeder
{
    private static readonly string[] FirstNamesFemale =
    [
        "Ana", "Maria", "Juliana", "Fernanda", "Camila", "Beatriz", "Larissa", "Patrícia",
        "Carla", "Renata", "Luciana", "Amanda", "Bianca", "Cláudia"
    ];

    private static readonly string[] FirstNamesMale =
    [
        "João", "Carlos", "Pedro", "Lucas", "Rafael", "Bruno", "Marcos", "André", "Felipe", "Ricardo",
        "Paulo", "Diego", "Gustavo", "Rodrigo"
    ];

    private static readonly string[] LastNames =
    [
        "Silva", "Santos", "Oliveira", "Souza", "Lima", "Costa", "Ferreira", "Rodrigues",
        "Almeida", "Pereira", "Carvalho", "Gomes", "Martins", "Ribeiro", "Barbosa", "Nascimento"
    ];

    private static readonly string[] Cities = ["São Paulo", "Campinas", "Santos", "Ribeirão Preto", "Sorocaba"];
    private static readonly string[] States = ["SP", "RJ", "MG", "PR", "RS"];
    private static readonly int[] AreaCodes = [11, 21, 31, 41, 51, 61, 71, 81, 19, 27, 47, 48, 85, 62, 83];

    private static readonly string[] Neighborhoods =
    [
        "Centro", "Jardim Paulista", "Vila Nova", "Boa Vista", "Santa Cruz", "Industrial", "Morumbi"
    ];

    private static readonly string[] StreetPrefixes = ["Rua", "Av.", "Trav.", "Al."];

    private static readonly string[] Complaints =
    [
        "Dor abdominal", "Cefaleia", "Febre", "Dispneia", "Tontura", "Dor torácica", "Trauma leve", "Náuseas"
    ];

    private static readonly (AppointmentKind Kind, string Reason)[] AppointmentTemplates =
    [
        (AppointmentKind.Consulta, "Consulta de rotina"),
        (AppointmentKind.Consulta, "Avaliação clínica geral"),
        (AppointmentKind.Retorno, "Retorno pós-consulta"),
        (AppointmentKind.Retorno, "Reavaliação de exames"),
        (AppointmentKind.Exame, "Solicitação de exame complementar"),
        (AppointmentKind.Exame, "Exame diagnóstico ambulatorial"),
        (AppointmentKind.Consulta, "Consulta especializada"),
        (AppointmentKind.Retorno, "Acompanhamento terapêutico")
    ];

    public static async Task<HospitalLoadDataResult> RunAsync(
        AppDbContext db,
        IFieldEncryptionService encryption,
        HospitalLoadDataOptions options,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!IsLoadSeedAllowed())
        {
            throw new InvalidOperationException(
                "Carga de dados bloqueada. Defina GTH_ALLOW_LOAD_SEED=true e use um banco de TESTE dedicado.");
        }

        if (!options.SkipMigrate)
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (options.ClearExisting)
        {
            var removed = await ClearMarkedDataAsync(db, cancellationToken);
            logger.LogWarning("Removidos {Count} pacientes marcados com {Marker}", removed, HospitalLoadDataOptions.MarkerPrefix);
        }

        var professionals = await db.Professionals.AsNoTracking().Select(p => p.Id).ToListAsync(cancellationToken);
        if (professionals.Count == 0)
        {
            throw new InvalidOperationException("Nenhum profissional cadastrado. Execute a API uma vez para aplicar seeds base.");
        }

        var labExamIds = await db.LabExamCatalogs.AsNoTracking().Select(e => e.Id).Take(50).ToListAsync(cancellationToken);
        var availableBeds = await db.Beds
            .AsNoTracking()
            .Where(b => b.Status == BedStatus.Available && b.IsActive)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var insuranceLookup = await LoadInsuranceLookupAsync(db, cancellationToken);

        var rnd = new Random(options.RandomSeed);
        var started = DateTime.UtcNow;
        var stats = new HospitalLoadDataResult();

        var existingCount = await db.Patients.CountAsync(
            p => p.Notes != null && p.Notes.StartsWith(HospitalLoadDataOptions.MarkerPrefix),
            cancellationToken);
        var sequenceOffset = existingCount;

        var slotAllocator = await CreateSlotAllocatorAsync(db, cancellationToken);

        var appointmentMin = Math.Max(1, options.AppointmentsPerPatientMin);
        var appointmentMax = Math.Max(appointmentMin, options.AppointmentsPerPatientMax);
        if (options.AppointmentsPerPatient > appointmentMax)
        {
            appointmentMax = options.AppointmentsPerPatient;
        }

        for (var batchStart = 0; batchStart < options.Patients; batchStart += options.BatchSize)
        {
            var batchCount = Math.Min(options.BatchSize, options.Patients - batchStart);
            var patients = new List<Patient>(batchCount);
            var patientInsurances = new List<PatientInsurance>();
            var visits = new List<EmergencyVisit>();
            var appointments = new List<Appointment>();
            var labOrders = new List<LabOrder>();
            var labItems = new List<LabOrderItem>();
            var pepEntries = new List<MedicalRecordEntry>();
            var hospitalizations = new List<Hospitalization>();
            var bedsToOccupy = new List<Guid>();

            for (var i = 0; i < batchCount; i++)
            {
                var globalIndex = sequenceOffset + batchStart + i;
                var cpf = GenerateValidCpf(globalIndex + options.RandomSeed);
                var gender = rnd.NextDouble() < 0.52 ? Gender.Female : Gender.Male;
                var firstNamePool = gender == Gender.Female ? FirstNamesFemale : FirstNamesMale;
                var fullName = $"{firstNamePool[globalIndex % firstNamePool.Length]} {LastNames[(globalIndex / firstNamePool.Length) % LastNames.Length]} {globalIndex:D5}";
                var birthDate = GenerateBirthDate(rnd);

                var patient = new Patient
                {
                    FullName = fullName,
                    BirthDate = birthDate,
                    Gender = gender,
                    Email = $"paciente{globalIndex:D6}@gth-load.local",
                    Phone = GenerateLandlinePhone(rnd),
                    MobilePhone = GenerateMobilePhone(rnd),
                    AddressStreet = $"{StreetPrefixes[globalIndex % StreetPrefixes.Length]} {LastNames[globalIndex % LastNames.Length]}, {rnd.Next(1, 999)}",
                    AddressNeighborhood = Neighborhoods[globalIndex % Neighborhoods.Length],
                    AddressZipCode = $"{rnd.Next(10000, 99999):D5}-{rnd.Next(100, 999):D3}",
                    AddressCity = Cities[globalIndex % Cities.Length],
                    AddressState = States[globalIndex % States.Length],
                    Notes = $"{HospitalLoadDataOptions.MarkerPrefix}|{globalIndex}",
                    MedicalRecord = new MedicalRecord
                    {
                        RecordNumber = $"LOAD-{DateTime.UtcNow:yyyy}-{globalIndex:D7}"
                    }
                };

                PatientFieldProtection.Protect(patient, encryption, cpf);
                patients.Add(patient);
            }

            db.Patients.AddRange(patients);
            await db.SaveChangesAsync(cancellationToken);
            stats.PatientsCreated += patients.Count;

            foreach (var patient in patients)
            {
                var insurance = PickInsurance(rnd, insuranceLookup);
                if (insurance is not null)
                {
                    patientInsurances.Add(new PatientInsurance
                    {
                        PatientId = patient.Id,
                        HealthInsuranceId = insurance.Id,
                        CardNumber = $"LOAD-{patient.Id:N}"[..20],
                        PlanName = insurance.Name == "SUS" ? "SUS" : "Plano ambulatorial",
                        IsPrimary = true,
                        ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2))
                    });
                }

                var recordId = patient.MedicalRecord!.Id;
                var professionalId = professionals[rnd.Next(professionals.Count)];

                for (var v = 0; v < options.VisitsPerPatient; v++)
                {
                    var arrived = DateTime.UtcNow.AddDays(-rnd.Next(1, 365)).AddHours(-rnd.Next(0, 23));
                    var status = (EmergencyVisitStatus)rnd.Next(1, 5);
                    visits.Add(new EmergencyVisit
                    {
                        PatientId = patient.Id,
                        ProfessionalId = professionalId,
                        ChiefComplaint = Complaints[rnd.Next(Complaints.Length)],
                        Urgency = (TriageUrgency)rnd.Next(1, 6),
                        Status = status,
                        ArrivedAt = arrived,
                        StartedAt = status != EmergencyVisitStatus.Waiting ? arrived.AddMinutes(rnd.Next(15, 90)) : null,
                        DischargedAt = status == EmergencyVisitStatus.Discharged ? arrived.AddHours(rnd.Next(2, 12)) : null,
                        Notes = HospitalLoadDataOptions.MarkerPrefix
                    });
                }

                var appointmentCount = rnd.Next(appointmentMin, appointmentMax + 1);
                for (var a = 0; a < appointmentCount; a++)
                {
                    var template = AppointmentTemplates[rnd.Next(AppointmentTemplates.Length)];
                    var durationMinutes = AppointmentDurationRules.GetMinutes(template.Kind);
                    var isFuture = a == appointmentCount - 1 && rnd.NextDouble() < 0.35;
                    var daysOffset = isFuture ? rnd.Next(1, 21) : -rnd.Next(1, 365);
                    var preferredStart = DateTime.UtcNow.Date.AddDays(daysOffset).AddHours(9 + rnd.Next(0, 7));

                    DateTime scheduledAt;
                    if (!slotAllocator.TryAllocateSlot(professionalId, preferredStart, template.Kind, out scheduledAt, rnd))
                    {
                        professionalId = professionals[rnd.Next(professionals.Count)];
                        if (!slotAllocator.TryAllocateSlot(professionalId, preferredStart, template.Kind, out scheduledAt, rnd))
                        {
                            continue;
                        }
                    }

                    var status = isFuture
                        ? (rnd.NextDouble() < 0.6 ? AppointmentStatus.Scheduled : AppointmentStatus.Confirmed)
                        : AppointmentStatus.Completed;

                    appointments.Add(new Appointment
                    {
                        PatientId = patient.Id,
                        ProfessionalId = professionalId,
                        ScheduledAt = scheduledAt,
                        DurationMinutes = durationMinutes,
                        Status = status,
                        Reason = $"{template.Reason} (dados de teste)",
                        Notes = HospitalLoadDataOptions.MarkerPrefix
                    });
                }

                if (labExamIds.Count > 0)
                {
                    for (var e = 0; e < options.ExamsPerPatient; e++)
                    {
                        var order = new LabOrder
                        {
                            PatientId = patient.Id,
                            RequestingProfessionalId = professionalId,
                            Status = (LabOrderStatus)rnd.Next(1, 5),
                            Notes = HospitalLoadDataOptions.MarkerPrefix
                        };
                        labOrders.Add(order);

                        var examId = labExamIds[rnd.Next(labExamIds.Count)];
                        labItems.Add(new LabOrderItem
                        {
                            LabOrder = order,
                            LabExamCatalogId = examId,
                            Status = LabItemStatus.Pending
                        });
                    }
                }

                for (var p = 0; p < options.PepEntriesPerPatient; p++)
                {
                    pepEntries.Add(new MedicalRecordEntry
                    {
                        MedicalRecordId = recordId,
                        ProfessionalId = professionalId,
                        EntryType = p % 2 == 0 ? MedicalRecordEntryType.Evolution : MedicalRecordEntryType.Anamnesis,
                        Content = $"Registro clínico fictício #{p + 1} — {HospitalLoadDataOptions.MarkerPrefix}",
                        IsSigned = p % 3 != 0,
                        SignedAt = p % 3 != 0 ? DateTime.UtcNow.AddDays(-rnd.Next(1, 30)) : null,
                        SignedByProfessionalId = p % 3 != 0 ? professionalId : null,
                        ClientRequestId = $"{HospitalLoadDataOptions.MarkerPrefix}-{patient.Id:N}-{p}"
                    });
                }

                if (availableBeds.Count > 0 && rnd.NextDouble() < options.HospitalizationRate)
                {
                    var bedId = availableBeds[^1];
                    availableBeds.RemoveAt(availableBeds.Count - 1);
                    bedsToOccupy.Add(bedId);

                    var admitted = DateTime.UtcNow.AddDays(-rnd.Next(1, 14));
                    hospitalizations.Add(new Hospitalization
                    {
                        PatientId = patient.Id,
                        BedId = bedId,
                        ProfessionalId = professionalId,
                        AdmittedAt = admitted,
                        Status = HospitalizationStatus.Active,
                        Reason = "Internação fictícia para teste de carga",
                        Notes = HospitalLoadDataOptions.MarkerPrefix
                    });
                }
            }

            if (patientInsurances.Count > 0)
            {
                db.PatientInsurances.AddRange(patientInsurances);
                stats.PatientInsurancesCreated += patientInsurances.Count;
            }

            if (visits.Count > 0)
            {
                db.EmergencyVisits.AddRange(visits);
                stats.VisitsCreated += visits.Count;
            }

            if (appointments.Count > 0)
            {
                db.Appointments.AddRange(appointments);
                stats.AppointmentsCreated += appointments.Count;
            }

            if (labOrders.Count > 0)
            {
                db.LabOrders.AddRange(labOrders);
                stats.LabOrdersCreated += labOrders.Count;
            }

            if (labItems.Count > 0)
            {
                db.LabOrderItems.AddRange(labItems);
                stats.LabOrderItemsCreated += labItems.Count;
            }

            if (pepEntries.Count > 0)
            {
                db.MedicalRecordEntries.AddRange(pepEntries);
                stats.PepEntriesCreated += pepEntries.Count;
            }

            if (hospitalizations.Count > 0)
            {
                db.Hospitalizations.AddRange(hospitalizations);
                stats.HospitalizationsCreated += hospitalizations.Count;

                foreach (var bedId in bedsToOccupy)
                {
                    await db.Beds
                        .Where(b => b.Id == bedId)
                        .ExecuteUpdateAsync(
                            s => s.SetProperty(b => b.Status, BedStatus.Occupied),
                            cancellationToken);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Lote {Batch}: +{Count} pacientes (total acumulado {Total})",
                batchStart / options.BatchSize + 1,
                batchCount,
                stats.PatientsCreated);
        }

        stats.Elapsed = DateTime.UtcNow - started;

        var queueStats = await SeedTodayQueuesAsync(
            db,
            professionals,
            rnd,
            options,
            logger,
            cancellationToken);
        stats.WaitingRoomAppointmentsCreated = queueStats.WaitingRoomAppointments;
        stats.TodayEmergencyVisitsCreated = queueStats.EmergencyVisits;

        return stats;
    }

    private const int WaitingRoomTargetCount = 40;
    private const int TodayEmergencyWaitingTarget = 15;

    /// <summary>Recria fila de sala de espera e PS para o dia atual (idempotente por dia).</summary>
    internal static async Task RefreshTodayQueuesAsync(
        AppDbContext db,
        HospitalLoadDataOptions options,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var marker = HospitalLoadDataOptions.MarkerPrefix;
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        await db.Appointments
            .Where(a => a.Notes != null && a.Notes.StartsWith(marker)
                && a.ScheduledAt >= todayStart && a.ScheduledAt < todayEnd)
            .ExecuteDeleteAsync(cancellationToken);

        await db.EmergencyVisits
            .Where(v => v.Notes != null && v.Notes.StartsWith(marker)
                && v.Status == EmergencyVisitStatus.Waiting
                && v.ArrivedAt >= todayStart && v.ArrivedAt < todayEnd)
            .ExecuteDeleteAsync(cancellationToken);

        var professionals = await db.Professionals.AsNoTracking().Select(p => p.Id).ToListAsync(cancellationToken);
        if (professionals.Count == 0)
        {
            return;
        }

        var rnd = new Random(options.RandomSeed + 4242);
        await SeedTodayQueuesAsync(db, professionals, rnd, options, logger, cancellationToken);
    }

    private static async Task<(int WaitingRoomAppointments, int EmergencyVisits)> SeedTodayQueuesAsync(
        AppDbContext db,
        IReadOnlyList<Guid> professionals,
        Random rnd,
        HospitalLoadDataOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var markedPatientIds = await db.Patients
            .AsNoTracking()
            .Where(p => p.Notes != null && p.Notes.StartsWith(HospitalLoadDataOptions.MarkerPrefix))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (markedPatientIds.Count == 0 || professionals.Count == 0)
        {
            return (0, 0);
        }

        var todayStart = DateTime.UtcNow.Date;
        var waitingAppointments = new List<Appointment>();
        var statusCycle = new[]
        {
            AppointmentStatus.Scheduled,
            AppointmentStatus.Confirmed,
            AppointmentStatus.InProgress
        };

        var occupiedSlots = new Dictionary<Guid, List<(DateTime Start, int Duration)>>();
        var patientPool = markedPatientIds.OrderBy(_ => rnd.Next()).ToList();
        var patientIndex = 0;
        var slotMinutes = 15;
        var dayStartMinutes = 11 * 60;
        var dayEndMinutes = 20 * 60;

        for (var i = 0; i < WaitingRoomTargetCount && patientPool.Count > 0; i++)
        {
            var patientId = patientPool[patientIndex % patientPool.Count];
            patientIndex++;
            var professionalId = professionals[rnd.Next(professionals.Count)];
            var durationMinutes = AppointmentDurationRules.GetMinutes(AppointmentKind.Consulta);

            if (!occupiedSlots.TryGetValue(professionalId, out var slots))
            {
                slots = [];
                occupiedSlots[professionalId] = slots;
            }

            DateTime? scheduledAt = null;
            for (var minute = dayStartMinutes; minute < dayEndMinutes; minute += slotMinutes)
            {
                var candidate = todayStart.AddMinutes(minute);
                var hasConflict = slots.Any(s =>
                    AppointmentSchedulingEngine.IntervalsOverlap(
                        s.Start, s.Duration, candidate, durationMinutes));

                if (!hasConflict)
                {
                    scheduledAt = candidate;
                    slots.Add((candidate, durationMinutes));
                    break;
                }
            }

            if (scheduledAt is null)
            {
                continue;
            }

            waitingAppointments.Add(new Appointment
            {
                PatientId = patientId,
                ProfessionalId = professionalId,
                ScheduledAt = scheduledAt.Value,
                DurationMinutes = durationMinutes,
                Status = statusCycle[i % statusCycle.Length],
                Reason = "Sala de espera — consulta do dia (dados de teste)",
                Notes = HospitalLoadDataOptions.MarkerPrefix
            });
        }

        if (waitingAppointments.Count > 0)
        {
            db.Appointments.AddRange(waitingAppointments);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Fila sala de espera: {Count} agendamentos para hoje ({Marker}).",
                waitingAppointments.Count,
                HospitalLoadDataOptions.MarkerPrefix);
        }

        var emergencyPatients = markedPatientIds.OrderBy(_ => rnd.Next()).Take(TodayEmergencyWaitingTarget).ToList();
        var emergencyVisits = new List<EmergencyVisit>();
        var emergencyProfessional = professionals[rnd.Next(professionals.Count)];

        for (var i = 0; i < emergencyPatients.Count; i++)
        {
            var arrivedAt = todayStart.AddHours(8).AddMinutes(rnd.Next(0, 9 * 60));
            emergencyVisits.Add(new EmergencyVisit
            {
                PatientId = emergencyPatients[i],
                ProfessionalId = emergencyProfessional,
                ChiefComplaint = Complaints[rnd.Next(Complaints.Length)],
                Urgency = (TriageUrgency)rnd.Next(1, 5),
                Status = EmergencyVisitStatus.Waiting,
                ArrivedAt = arrivedAt,
                Notes = HospitalLoadDataOptions.MarkerPrefix
            });
        }

        if (emergencyVisits.Count > 0)
        {
            db.EmergencyVisits.AddRange(emergencyVisits);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Fila PS: {Count} visitas aguardando hoje ({Marker}).",
                emergencyVisits.Count,
                HospitalLoadDataOptions.MarkerPrefix);
        }

        return (waitingAppointments.Count, emergencyVisits.Count);
    }

    private static async Task<ProfessionalSlotAllocator> CreateSlotAllocatorAsync(
        AppDbContext db,
        CancellationToken cancellationToken,
        SchedulingBusinessHours? businessHours = null)
    {
        var allocator = new ProfessionalSlotAllocator(businessHours);

        var existing = await db.Appointments
            .AsNoTracking()
            .Where(a => a.IsActive
                && a.Status != AppointmentStatus.Cancelled
                && a.Status != AppointmentStatus.NoShow)
            .Select(a => new
            {
                a.ProfessionalId,
                a.ScheduledAt,
                a.DurationMinutes,
                a.Status,
                a.IsActive
            })
            .ToListAsync(cancellationToken);

        foreach (var appointment in existing)
        {
            allocator.RegisterSlot(
                appointment.ProfessionalId,
                appointment.ScheduledAt,
                appointment.DurationMinutes,
                appointment.Status,
                appointment.IsActive);
        }

        return allocator;
    }

    private sealed record InsurancePick(Guid Id, string Name);

    private sealed class InsuranceLookup
    {
        public required HealthInsurance Sus { get; init; }
        public required HealthInsurance Particular { get; init; }
        public required IReadOnlyList<HealthInsurance> PrivatePlans { get; init; }
    }

    private static async Task<InsuranceLookup> LoadInsuranceLookupAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var all = await db.HealthInsurances.AsNoTracking().ToListAsync(cancellationToken);

        var sus = all.FirstOrDefault(i => i.Name.Equals("SUS", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Convênio SUS não encontrado. Execute seeds base.");
        var particular = all.FirstOrDefault(i => i.Name.Equals("Particular", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Convênio Particular não encontrado. Execute seeds base.");

        var privatePlans = all
            .Where(i => !i.Name.Equals("SUS", StringComparison.OrdinalIgnoreCase)
                && !i.Name.Equals("Particular", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (privatePlans.Count == 0)
        {
            privatePlans = [particular];
        }

        return new InsuranceLookup
        {
            Sus = sus,
            Particular = particular,
            PrivatePlans = privatePlans
        };
    }

    private static InsurancePick? PickInsurance(Random rnd, InsuranceLookup lookup)
    {
        var roll = rnd.NextDouble();
        if (roll < 0.40)
        {
            return new InsurancePick(lookup.Sus.Id, lookup.Sus.Name);
        }

        if (roll < 0.90)
        {
            var plan = lookup.PrivatePlans[rnd.Next(lookup.PrivatePlans.Count)];
            return new InsurancePick(plan.Id, plan.Name);
        }

        return new InsurancePick(lookup.Particular.Id, lookup.Particular.Name);
    }

    internal static DateOnly GenerateBirthDate(Random rnd)
    {
        var band = rnd.NextDouble();
        if (band < 0.15)
        {
            return DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-rnd.Next(0, 13)).AddDays(-rnd.Next(0, 365)));
        }

        if (band < 0.85)
        {
            return DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-rnd.Next(13, 60)).AddDays(-rnd.Next(0, 365)));
        }

        return DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-rnd.Next(60, 95)).AddDays(-rnd.Next(0, 365)));
    }

    internal static string GenerateMobilePhone(Random rnd)
    {
        var ddd = AreaCodes[rnd.Next(AreaCodes.Length)];
        return $"({ddd:D2}) 9{rnd.Next(1000, 9999):D4}-{rnd.Next(1000, 9999):D4}";
    }

    internal static string GenerateLandlinePhone(Random rnd)
    {
        var ddd = AreaCodes[rnd.Next(AreaCodes.Length)];
        return $"({ddd:D2}) {rnd.Next(2000, 4999):D4}-{rnd.Next(1000, 9999):D4}";
    }

    public static bool IsLoadSeedAllowed()
        => string.Equals(Environment.GetEnvironmentVariable("GTH_ALLOW_LOAD_SEED"), "true", StringComparison.OrdinalIgnoreCase);

    public static async Task<int> ClearMarkedDataAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!IsLoadSeedAllowed())
        {
            throw new InvalidOperationException("GTH_ALLOW_LOAD_SEED=true é obrigatório para limpar dados de carga.");
        }

        var patientIds = await db.Patients
            .Where(p => p.Notes != null && p.Notes.StartsWith(HospitalLoadDataOptions.MarkerPrefix))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        await ClearMarkedFinancialDataAsync(db, patientIds, cancellationToken);

        if (patientIds.Count == 0)
        {
            return 0;
        }

        var recordIds = await db.MedicalRecords
            .Where(r => patientIds.Contains(r.PatientId))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var labOrderIds = await db.LabOrders
            .Where(o => patientIds.Contains(o.PatientId))
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        await db.PatientInsurances
            .Where(i => patientIds.Contains(i.PatientId))
            .ExecuteDeleteAsync(cancellationToken);

        if (labOrderIds.Count > 0)
        {
            var itemIds = await db.LabOrderItems
                .Where(i => labOrderIds.Contains(i.LabOrderId))
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);

            if (itemIds.Count > 0)
            {
                await db.LabResults.Where(r => itemIds.Contains(r.LabOrderItemId)).ExecuteDeleteAsync(cancellationToken);
                await db.LabOrderItems.Where(i => itemIds.Contains(i.Id)).ExecuteDeleteAsync(cancellationToken);
            }

            await db.LabOrders.Where(o => labOrderIds.Contains(o.Id)).ExecuteDeleteAsync(cancellationToken);
        }

        if (recordIds.Count > 0)
        {
            await db.MedicalRecordEntries.Where(e => recordIds.Contains(e.MedicalRecordId)).ExecuteDeleteAsync(cancellationToken);
        }

        await db.Hospitalizations.Where(h => patientIds.Contains(h.PatientId)).ExecuteDeleteAsync(cancellationToken);
        await db.EmergencyVisits.Where(v => patientIds.Contains(v.PatientId)).ExecuteDeleteAsync(cancellationToken);
        await db.Appointments.Where(a => patientIds.Contains(a.PatientId)).ExecuteDeleteAsync(cancellationToken);

        var bedIds = await db.Beds.Where(b => b.Status == BedStatus.Occupied).Select(b => b.Id).ToListAsync(cancellationToken);
        if (bedIds.Count > 0)
        {
            await db.Beds
                .Where(b => bedIds.Contains(b.Id) && !db.Hospitalizations.Any(h => h.BedId == b.Id && h.Status == HospitalizationStatus.Active))
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BedStatus.Available), cancellationToken);
        }

        await db.MedicalRecords.Where(r => patientIds.Contains(r.PatientId)).ExecuteDeleteAsync(cancellationToken);

        await db.ConnectScheduledMessages
            .Where(m => m.PatientId != null && patientIds.Contains(m.PatientId.Value))
            .ExecuteDeleteAsync(cancellationToken);

        await db.ConnectWaitlistEntries
            .Where(m => patientIds.Contains(m.PatientId))
            .ExecuteDeleteAsync(cancellationToken);

        await db.Patients.Where(p => patientIds.Contains(p.Id)).ExecuteDeleteAsync(cancellationToken);

        return patientIds.Count;
    }

    internal static async Task ClearMarkedFinancialDataAsync(
        AppDbContext db,
        IReadOnlyList<Guid> patientIds,
        CancellationToken cancellationToken)
    {
        var marker = HospitalLoadDataOptions.MarkerPrefix;

        await ClearMarkedOperationsDataAsync(db, patientIds, cancellationToken);

        var markedGuideIds = await db.TissGuides
            .Where(g => g.Notes != null && g.Notes.StartsWith(marker))
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);

        var markedBatchIds = await db.TissBatches
            .Where(b => b.BatchNumber.StartsWith("GTH-LOAD-"))
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var financialAccountIds = await db.FinancialAccounts
            .Where(f =>
                (f.Notes != null && f.Notes.StartsWith(marker))
                || (f.PatientId != null && patientIds.Contains(f.PatientId.Value))
                || (f.TissGuideId != null && markedGuideIds.Contains(f.TissGuideId.Value)))
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        if (financialAccountIds.Count > 0)
        {
            var paymentIds = await db.FinancialPayments
                .Where(p => financialAccountIds.Contains(p.FinancialAccountId))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            if (paymentIds.Count > 0)
            {
                await db.FinancialPaymentInstallments
                    .Where(i => paymentIds.Contains(i.FinancialPaymentId))
                    .ExecuteDeleteAsync(cancellationToken);
                await db.FinancialPayments
                    .Where(p => paymentIds.Contains(p.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await db.FinancialAccountLineItems
                .Where(i => financialAccountIds.Contains(i.FinancialAccountId))
                .ExecuteDeleteAsync(cancellationToken);

            await db.FinancialAccounts
                .Where(f => financialAccountIds.Contains(f.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (markedGuideIds.Count > 0)
        {
            await db.InsuranceAuthorizations
                .Where(a => a.TissGuideId != null && markedGuideIds.Contains(a.TissGuideId.Value))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.TissGuideId, (Guid?)null),
                    cancellationToken);

            await db.TissGlosas
                .Where(g => markedGuideIds.Contains(g.TissGuideId))
                .ExecuteDeleteAsync(cancellationToken);
            await db.TissGuideItems
                .Where(i => markedGuideIds.Contains(i.TissGuideId))
                .ExecuteDeleteAsync(cancellationToken);
            await db.TissGuideAnnexes
                .Where(a => markedGuideIds.Contains(a.TissGuideId))
                .ExecuteDeleteAsync(cancellationToken);
            await db.TissGuides
                .Where(g => markedGuideIds.Contains(g.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (markedBatchIds.Count > 0)
        {
            await db.TissBatches
                .Where(b => markedBatchIds.Contains(b.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    internal static async Task ClearMarkedOperationsDataAsync(
        AppDbContext db,
        IReadOnlyList<Guid> patientIds,
        CancellationToken cancellationToken)
    {
        var marker = HospitalLoadDataOptions.MarkerPrefix;
        const string skuPrefix = "GTH-LOAD-";

        var dispensingIds = await db.PharmacyDispensings
            .Where(d => d.Notes != null && d.Notes.StartsWith(marker))
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        if (dispensingIds.Count > 0)
        {
            await db.PharmacyBillingEntries
                .Where(b => dispensingIds.Contains(b.DispensingId))
                .ExecuteDeleteAsync(cancellationToken);
            await db.PharmacyDispensingReversals
                .Where(r => dispensingIds.Contains(r.DispensingId))
                .ExecuteDeleteAsync(cancellationToken);
            await db.PharmacyDispensings
                .Where(d => dispensingIds.Contains(d.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await db.StockMovements
            .Where(m => m.Reference != null && m.Reference.StartsWith(marker))
            .ExecuteDeleteAsync(cancellationToken);

        var loadProductIds = await db.Products
            .Where(p => p.Sku.StartsWith(skuPrefix))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (loadProductIds.Count > 0)
        {
            await db.ProductBillingRules
                .Where(r => loadProductIds.Contains(r.ProductId))
                .ExecuteDeleteAsync(cancellationToken);
            await db.WardStockMovements
                .Where(m => loadProductIds.Contains(m.ProductId))
                .ExecuteDeleteAsync(cancellationToken);
            await db.WardStockBalances
                .Where(b => loadProductIds.Contains(b.ProductId))
                .ExecuteDeleteAsync(cancellationToken);
            await db.Products
                .Where(p => loadProductIds.Contains(p.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await db.MiscellaneousReceipts
            .Where(r => r.ReceiptNumber.StartsWith(skuPrefix))
            .ExecuteDeleteAsync(cancellationToken);

        await db.FinancialCashSessions
            .Where(s => s.Notes != null && s.Notes.StartsWith(marker))
            .ExecuteDeleteAsync(cancellationToken);

        var payrollRunIds = await db.PayrollRuns
            .Where(r => r.Notes != null && r.Notes.StartsWith(marker))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (payrollRunIds.Count > 0)
        {
            var payrollItemIds = await db.PayrollItems
                .Where(i => payrollRunIds.Contains(i.PayrollRunId))
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);

            if (payrollItemIds.Count > 0)
            {
                await db.PayrollItemLines
                    .Where(l => payrollItemIds.Contains(l.PayrollItemId))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await db.PayrollItems
                .Where(i => payrollRunIds.Contains(i.PayrollRunId))
                .ExecuteDeleteAsync(cancellationToken);
            await db.PayrollRuns
                .Where(r => payrollRunIds.Contains(r.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (patientIds.Count > 0)
        {
            await db.TpaClaims
                .Where(c => c.Notes != null && c.Notes.StartsWith(marker)
                    && patientIds.Contains(c.PatientId))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    internal static string GenerateValidCpf(int seed)
    {
        var rnd = new Random(seed);
        var digits = new int[9];
        for (var i = 0; i < 9; i++)
        {
            digits[i] = rnd.Next(10);
        }

        if (digits.Distinct().Count() == 1)
        {
            digits[8] = (digits[8] + 1) % 10;
        }

        var body = string.Concat(digits.Select(d => d.ToString()));
        var d1 = ComputeCpfCheckDigit(digits, 10);
        var d2 = ComputeCpfCheckDigit(digits.Concat([d1]).ToArray(), 11);
        var cpf = body + d1 + d2;

        if (!PatientCpfRules.IsValidChecksum(cpf))
        {
            return GenerateValidCpf(seed + 7919);
        }

        return cpf;
    }

    private static int ComputeCpfCheckDigit(int[] digits, int weightStart)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            sum += digits[i] * (weightStart - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}

public sealed class HospitalLoadDataResult
{
    public int PatientsCreated { get; set; }
    public int PatientInsurancesCreated { get; set; }
    public int VisitsCreated { get; set; }
    public int AppointmentsCreated { get; set; }
    public int LabOrdersCreated { get; set; }
    public int LabOrderItemsCreated { get; set; }
    public int PepEntriesCreated { get; set; }
    public int HospitalizationsCreated { get; set; }
    public int WaitingRoomAppointmentsCreated { get; set; }
    public int TodayEmergencyVisitsCreated { get; set; }
    public TimeSpan Elapsed { get; set; }
}
