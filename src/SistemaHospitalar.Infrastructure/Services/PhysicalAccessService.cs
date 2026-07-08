using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.PhysicalAccess;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Messaging;
using SistemaHospitalar.Infrastructure.Persistence;
using SistemaHospitalar.Infrastructure.PhysicalAccess;

namespace SistemaHospitalar.Infrastructure.Services;

public class PhysicalAccessService(
    AppDbContext dbContext,
    HospitalEventPublisher eventPublisher) : IPhysicalAccessService
{
    public const string QrAppointmentPrefix = "HMS-APT:";
    public const string QrCompanionPrefix = "HMS-CMP:";
    public const string QrEmployeePrefix = "HMS-EMP:";
    public const int MaxCompanionsPerPatient = 2;
    public static readonly TimeSpan AppointmentEarlyWindow = TimeSpan.FromHours(1);
    public static readonly TimeSpan AppointmentLateWindow = TimeSpan.FromMinutes(30);

    public async Task<PhysicalAccessDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeedDataAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;
        var grantedToday = await dbContext.AccessControlRecords
            .CountAsync(r => r.IsActive && r.OccurredAt >= today && r.Result == AccessValidationResult.Granted, cancellationToken);
        var deniedToday = await dbContext.AccessControlRecords
            .CountAsync(r => r.IsActive && r.OccurredAt >= today && r.Result != AccessValidationResult.Granted, cancellationToken);

        var visitorsInside = await dbContext.VisitorLogs
            .CountAsync(v => v.IsActive && v.Status == VisitorLogStatus.Inside, cancellationToken);
        var activeCompanions = await dbContext.AccessCredentials
            .CountAsync(c => c.IsActive && c.Status == AccessCredentialStatus.Active && c.PersonType == AccessPersonType.Companion, cancellationToken);
        var vehiclesInside = await dbContext.ParkingSessions
            .CountAsync(s => s.IsActive && s.Status == ParkingSessionStatus.Active, cancellationToken);
        var facialCount = await dbContext.FacialBiometricTemplates
            .CountAsync(f => f.IsActive && f.Status == FacialBiometricStatus.Active, cancellationToken);

        var recentAccess = await dbContext.AccessControlRecords
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.OccurredAt)
            .Take(15)
            .Select(r => new AccessControlRecordDto(
                r.Id, r.PersonType, r.PersonName, r.Method, r.Direction,
                r.Result, r.Location, r.Details, r.OccurredAt))
            .ToListAsync(cancellationToken);

        var recentLpr = await GetLprEventsInternalAsync(10, cancellationToken);

        return new PhysicalAccessDashboardDto(
            visitorsInside + activeCompanions,
            grantedToday,
            deniedToday,
            activeCompanions,
            vehiclesInside,
            facialCount,
            recentAccess,
            recentLpr);
    }

    public async Task<IReadOnlyList<AccessZoneDto>> GetZonesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeedDataAsync(cancellationToken);
        return await dbContext.AccessZones
            .AsNoTracking()
            .Where(z => z.IsActive)
            .OrderBy(z => z.Name)
            .Select(z => new AccessZoneDto(z.Id, z.Code, z.Name, z.Building, z.Floor, z.RequiresAuthorization))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccessTurnstileDto>> GetTurnstilesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeedDataAsync(cancellationToken);
        return await dbContext.AccessTurnstiles
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Include(t => t.AccessZone)
            .OrderBy(t => t.Name)
            .Select(t => new AccessTurnstileDto(
                t.Id, t.Code, t.Name, t.AccessZoneId,
                t.AccessZone != null ? t.AccessZone.Name : null,
                t.IntegrationVendor, t.IsEntry))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccessControlRecordDto>> GetAccessRecordsAsync(
        int? limit, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit ?? 100, 1, 500);
        return await dbContext.AccessControlRecords
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.OccurredAt)
            .Take(take)
            .Select(r => new AccessControlRecordDto(
                r.Id, r.PersonType, r.PersonName, r.Method, r.Direction,
                r.Result, r.Location, r.Details, r.OccurredAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccessCredentialDto>> GetCredentialsAsync(CancellationToken cancellationToken = default)
        => await dbContext.AccessCredentials
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Include(c => c.AllowedZone)
            .OrderByDescending(c => c.CreatedAt)
            .Take(100)
            .Select(c => new AccessCredentialDto(
                c.Id, c.PersonType, c.HolderName, c.CredentialType, c.Status,
                c.Token, c.AllowedZone != null ? c.AllowedZone.Name : null, c.ValidUntil))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FacialBiometricDto>> GetFacialEnrollmentsAsync(CancellationToken cancellationToken = default)
        => await dbContext.FacialBiometricTemplates
            .AsNoTracking()
            .Where(f => f.IsActive)
            .OrderByDescending(f => f.EnrolledAt)
            .Take(100)
            .Select(f => new FacialBiometricDto(
                f.Id, f.PersonType, f.PersonName, f.Status, f.EnrolledAt, f.PhotoData != null))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RegisteredVehicleDto>> GetVehiclesAsync(CancellationToken cancellationToken = default)
        => await dbContext.RegisteredVehicles
            .AsNoTracking()
            .Where(v => v.IsActive)
            .OrderBy(v => v.Plate)
            .Select(v => new RegisteredVehicleDto(
                v.Id, v.Plate, v.Model, v.Color, v.OwnerCategory, v.OwnerName, v.ParkingExempt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LprReadEventDto>> GetLprEventsAsync(CancellationToken cancellationToken = default)
        => await GetLprEventsInternalAsync(50, cancellationToken);

    public async Task<IReadOnlyList<KioskTicketDto>> GetKioskTicketsAsync(
        bool? pendingOnly, CancellationToken cancellationToken = default)
    {
        var query = dbContext.KioskTickets.AsNoTracking().Where(t => t.IsActive);
        if (pendingOnly == true)
        {
            query = query.Where(t => !t.Called);
        }

        return await query
            .OrderByDescending(t => t.IssuedAt)
            .Take(100)
            .Select(t => new KioskTicketDto(
                t.Id, t.TicketType, t.TicketNumber, t.PatientName, t.Sector, t.IssuedAt, t.Called))
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<AccessIntegrationProfileDto>> GetIntegrationProfilesAsync()
        => Task.FromResult(AccessIntegrationProfiles.All);

    public async Task<IReadOnlyList<EmployeeSectorAccessDto>> GetEmployeeAccessAsync(CancellationToken cancellationToken = default)
    {
        var employees = await dbContext.Employees
            .AsNoTracking()
            .Where(e => e.IsActive)
            .Include(e => e.Department)
            .Include(e => e.Shifts)
            .OrderBy(e => e.FullName)
            .Take(50)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = new List<EmployeeSectorAccessDto>();

        foreach (var emp in employees)
        {
            var onShift = emp.Shifts.Any(s => s.IsActive && s.ShiftDate == today);
            var lastAccess = await dbContext.AccessControlRecords
                .AsNoTracking()
                .Where(r => r.EmployeeId == emp.Id && r.Result == AccessValidationResult.Granted)
                .OrderByDescending(r => r.OccurredAt)
                .Select(r => (DateTime?)r.OccurredAt)
                .FirstOrDefaultAsync(cancellationToken);

            result.Add(new EmployeeSectorAccessDto(
                emp.Id,
                emp.FullName,
                emp.Department.Name,
                MapDepartmentToZone(emp.Department.Name),
                onShift,
                lastAccess));
        }

        return result;
    }

    public async Task<AppointmentQrDto?> GetAppointmentQrAsync(
        Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appt = await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.IsActive, cancellationToken);

        if (appt is null)
        {
            return null;
        }

        return new AppointmentQrDto(
            appt.Id,
            BuildAppointmentQr(appt.Id),
            appt.Patient.FullName,
            appt.ScheduledAt);
    }

    public async Task<TurnstileValidationResultDto> ValidateTurnstileAsync(
        TurnstileValidationRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureSeedDataAsync(cancellationToken);

        var turnstile = await dbContext.AccessTurnstiles
            .Include(t => t.AccessZone)
            .FirstOrDefaultAsync(t => t.Code == request.TurnstileCode.Trim() && t.IsActive, cancellationToken);

        if (turnstile is null)
        {
            return new TurnstileValidationResultDto(false, AccessValidationResult.Denied, "Catraca não cadastrada.", null, null, null);
        }

        if (request.Method == AccessMethod.Facial)
        {
            return await ValidateFacialAccessAsync(new FacialValidationRequest(
                request.TurnstileCode, null, null, request.Payload), cancellationToken);
        }

        var payload = request.Payload.Trim();

        if (payload.StartsWith(QrAppointmentPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return await ValidatePatientAppointmentAsync(turnstile, request, payload, cancellationToken);
        }

        if (payload.StartsWith(QrCompanionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return await ValidateCompanionCredentialAsync(turnstile, request, payload, cancellationToken);
        }

        if (payload.StartsWith(QrEmployeePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return await ValidateEmployeeAccessAsync(turnstile, request, payload, cancellationToken);
        }

        var rfidCredential = await dbContext.AccessCredentials
            .FirstOrDefaultAsync(c => c.IsActive && c.Status == AccessCredentialStatus.Active
                && c.Token == payload, cancellationToken);

        if (rfidCredential is not null)
        {
            return await ValidateCredentialAsync(turnstile, request, rfidCredential, cancellationToken);
        }

        var denied = await RecordAccessAsync(
            turnstile, AccessPersonType.Visitor, "Desconhecido", null, null, null, null,
            request.Method, request.Direction, AccessValidationResult.Denied,
            "Credencial não reconhecida.", cancellationToken);

        return new TurnstileValidationResultDto(false, AccessValidationResult.Denied, "Credencial não reconhecida.", null, null, denied.Id);
    }

    public async Task<AccessCredentialDto> IssueCompanionCredentialAsync(
        IssueCompanionCredentialRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Paciente não encontrado.");

        var activeCompanions = await dbContext.AccessCredentials
            .CountAsync(c => c.IsActive && c.Status == AccessCredentialStatus.Active
                && c.PatientId == request.PatientId && c.PersonType == AccessPersonType.Companion, cancellationToken);

        if (activeCompanions >= MaxCompanionsPerPatient)
        {
            throw new InvalidOperationException($"Limite de {MaxCompanionsPerPatient} acompanhantes ativos por paciente.");
        }

        var credential = new AccessCredential
        {
            PersonType = AccessPersonType.Companion,
            HolderName = request.CompanionName.Trim(),
            PatientId = request.PatientId,
            CredentialType = request.CredentialType,
            AllowedZoneId = request.AllowedZoneId,
            VisitStartTime = request.VisitStartTime,
            VisitEndTime = request.VisitEndTime,
            ValidUntil = request.ValidUntil ?? DateTime.UtcNow.Date.AddDays(1).AddHours(23),
            Token = request.CredentialType == AccessCredentialType.Rfid
                ? $"RFID-{Guid.NewGuid():N}"[..16].ToUpperInvariant()
                : $"{QrCompanionPrefix}{Guid.NewGuid():D}"
        };

        dbContext.AccessCredentials.Add(credential);
        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("access.companion.credential", new
        {
            credential.Id,
            credential.HolderName,
            PatientId = request.PatientId,
            credential.Token
        }, cancellationToken);

        return (await GetCredentialsAsync(cancellationToken)).First(c => c.Id == credential.Id);
    }

    public async Task<FacialBiometricDto> EnrollFacialAsync(
        EnrollFacialRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TemplatePayload) && string.IsNullOrWhiteSpace(request.PhotoData))
        {
            throw new InvalidOperationException("Informe foto ou template biométrico.");
        }

        var hash = ComputeTemplateHash(request.TemplatePayload ?? request.PhotoData!);

        var template = new FacialBiometricTemplate
        {
            PersonType = request.PersonType,
            PersonName = request.PersonName.Trim(),
            PatientId = request.PatientId,
            EmployeeId = request.EmployeeId,
            ProfessionalId = request.ProfessionalId,
            TemplateHash = hash,
            PhotoData = NormalizePhoto(request.PhotoData),
            Status = FacialBiometricStatus.Active
        };

        dbContext.FacialBiometricTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetFacialEnrollmentsAsync(cancellationToken)).First(f => f.Id == template.Id);
    }

    public async Task<TurnstileValidationResultDto> ValidateFacialAccessAsync(
        FacialValidationRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureSeedDataAsync(cancellationToken);

        var turnstile = await dbContext.AccessTurnstiles
            .Include(t => t.AccessZone)
            .FirstOrDefaultAsync(t => t.Code == request.TurnstileCode.Trim() && t.IsActive, cancellationToken);

        if (turnstile is null)
        {
            return new TurnstileValidationResultDto(false, AccessValidationResult.Denied, "Catraca não cadastrada.", null, null, null);
        }

        FacialBiometricTemplate? match = null;

        if (request.PersonId.HasValue && request.PersonType.HasValue)
        {
            match = await dbContext.FacialBiometricTemplates
                .FirstOrDefaultAsync(f => f.IsActive && f.Status == FacialBiometricStatus.Active
                    && ((f.PatientId == request.PersonId && request.PersonType == AccessPersonType.Patient)
                        || (f.EmployeeId == request.PersonId && request.PersonType == AccessPersonType.Employee)), cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(request.TemplatePayload))
        {
            var hash = ComputeTemplateHash(request.TemplatePayload);
            match = await dbContext.FacialBiometricTemplates
                .FirstOrDefaultAsync(f => f.IsActive && f.Status == FacialBiometricStatus.Active && f.TemplateHash == hash, cancellationToken);
        }

        if (match is null)
        {
            var denied = await RecordAccessAsync(
                turnstile, AccessPersonType.Visitor, "Facial não identificado", null, null, null, null,
                AccessMethod.Facial, AccessDirection.Entry, AccessValidationResult.Denied,
                "Template facial não encontrado.", cancellationToken);
            return new TurnstileValidationResultDto(false, AccessValidationResult.Denied, "Rosto não reconhecido.", null, null, denied.Id);
        }

        if (turnstile.AccessZone?.RequiresAuthorization == true
            && match.PersonType is AccessPersonType.Visitor or AccessPersonType.Companion)
        {
            var zoneDenied = await RecordAccessAsync(
                turnstile, match.PersonType, match.PersonName, match.PatientId, match.EmployeeId, null, null,
                AccessMethod.Facial, AccessDirection.Entry, AccessValidationResult.WrongZone,
                $"Setor restrito: {turnstile.AccessZone.Name}", cancellationToken);
            return new TurnstileValidationResultDto(false, AccessValidationResult.WrongZone,
                $"Acesso negado ao setor {turnstile.AccessZone.Name}.", match.PersonName, match.PersonType, zoneDenied.Id);
        }

        var record = await RecordAccessAsync(
            turnstile, match.PersonType, match.PersonName, match.PatientId, match.EmployeeId, null, null,
            AccessMethod.Facial, AccessDirection.Entry, AccessValidationResult.Granted,
            "Acesso facial validado.", cancellationToken);

        return new TurnstileValidationResultDto(true, AccessValidationResult.Granted, "Acesso liberado.", match.PersonName, match.PersonType, record.Id);
    }

    public async Task<KioskCheckInResultDto> KioskCheckInAsync(
        KioskCheckInRequest request, CancellationToken cancellationToken = default)
    {
        Appointment? appointment = null;

        if (!string.IsNullOrWhiteSpace(request.QrPayload))
        {
            var apptId = ParseGuidPayload(request.QrPayload, QrAppointmentPrefix);
            if (apptId.HasValue)
            {
                appointment = await dbContext.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == apptId.Value && a.IsActive, cancellationToken);
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.Cpf))
        {
            var cpf = NormalizeCpf(request.Cpf);
            var patient = await dbContext.Patients
                .FirstOrDefaultAsync(p => p.IsActive && p.Cpf != null && p.Cpf.Replace(".", "").Replace("-", "") == cpf, cancellationToken);

            if (patient is not null)
            {
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);
                appointment = await dbContext.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.PatientId == patient.Id && a.IsActive
                        && a.ScheduledAt >= today && a.ScheduledAt < tomorrow
                        && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Completed)
                    .OrderBy(a => a.ScheduledAt)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }
        else if (request.FacialTemplateId.HasValue)
        {
            var facial = await dbContext.FacialBiometricTemplates
                .FirstOrDefaultAsync(f => f.Id == request.FacialTemplateId.Value && f.IsActive, cancellationToken);

            if (facial?.PatientId is not null)
            {
                var today = DateTime.UtcNow.Date;
                appointment = await dbContext.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.PatientId == facial.PatientId && a.IsActive
                        && a.ScheduledAt >= today && a.ScheduledAt < today.AddDays(1)
                        && a.Status != AppointmentStatus.Cancelled)
                    .OrderBy(a => a.ScheduledAt)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        if (appointment is null)
        {
            return new KioskCheckInResultDto(false, "Nenhum agendamento encontrado para hoje.", null, null, null);
        }

        if (!IsWithinAppointmentWindow(appointment.ScheduledAt))
        {
            return new KioskCheckInResultDto(false, "Fora da janela de horário permitida para check-in.", appointment.Id, appointment.Patient.FullName, null);
        }

        if (appointment.Status == AppointmentStatus.Scheduled)
        {
            appointment.Status = AppointmentStatus.Confirmed;
        }

        if (appointment.Status is AppointmentStatus.Confirmed or AppointmentStatus.Scheduled)
        {
            appointment.Status = AppointmentStatus.InProgress;
        }

        appointment.UpdatedAt = DateTime.UtcNow;

        var ticket = await IssueKioskTicketInternalAsync(
            KioskTicketType.Consultation,
            appointment.PatientId,
            appointment.Patient.FullName,
            appointment.Room ?? "Ambulatório",
            appointment.Id,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync("access.kiosk.checkin", new
        {
            appointment.Id,
            appointment.PatientId,
            ticket.TicketNumber
        }, cancellationToken);

        return new KioskCheckInResultDto(true, "Check-in confirmado. Senha emitida.", appointment.Id, appointment.Patient.FullName, ticket);
    }

    public async Task<KioskTicketDto> IssueKioskTicketAsync(
        IssueKioskTicketRequest request, CancellationToken cancellationToken = default)
    {
        var ticket = await IssueKioskTicketInternalAsync(
            request.TicketType,
            request.PatientId,
            request.PatientName,
            request.Sector,
            null,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    public async Task<RegisteredVehicleDto> RegisterVehicleAsync(
        RegisterVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var plate = request.Plate.Trim().ToUpperInvariant();
        var exists = await dbContext.RegisteredVehicles
            .AnyAsync(v => v.IsActive && v.Plate == plate, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Placa já cadastrada.");
        }

        var vehicle = new RegisteredVehicle
        {
            Plate = plate,
            Model = request.Model?.Trim(),
            Color = request.Color?.Trim(),
            OwnerCategory = request.OwnerCategory,
            OwnerName = request.OwnerName.Trim(),
            PatientId = request.PatientId,
            EmployeeId = request.EmployeeId,
            ParkingExempt = request.ParkingExempt
        };

        dbContext.RegisteredVehicles.Add(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetVehiclesAsync(cancellationToken)).First(v => v.Id == vehicle.Id);
    }

    public async Task<LprReadResultDto> ProcessLprReadAsync(
        LprReadRequest request, CancellationToken cancellationToken = default)
    {
        var plate = request.Plate.Trim().ToUpperInvariant();
        var vehicle = await dbContext.RegisteredVehicles
            .FirstOrDefaultAsync(v => v.IsActive && v.Plate == plate, cancellationToken);

        var gateOpened = false;
        string message;

        if (request.Direction == AccessDirection.Entry)
        {
            if (vehicle is not null)
            {
                gateOpened = true;
                message = $"Entrada autorizada — {vehicle.OwnerName} ({vehicle.OwnerCategory}).";
            }
            else
            {
                message = "Veículo não cadastrado — registro manual necessário.";
            }
        }
        else
        {
            var activeSession = await dbContext.ParkingSessions
                .FirstOrDefaultAsync(s => s.IsActive && s.VehiclePlate == plate && s.Status == ParkingSessionStatus.Active, cancellationToken);

            if (vehicle?.ParkingExempt == true || activeSession?.IsPaid == true)
            {
                gateOpened = true;
                message = vehicle?.ParkingExempt == true
                    ? "Saída liberada — isenção de estacionamento."
                    : "Saída liberada — pagamento confirmado.";
            }
            else if (activeSession is not null)
            {
                message = "Saída bloqueada — estacionamento não pago.";
            }
            else
            {
                gateOpened = true;
                message = "Saída registrada.";
            }
        }

        var lprEvent = new LprReadEvent
        {
            Plate = plate,
            CameraLocation = request.CameraLocation.Trim(),
            Direction = request.Direction,
            GateOpened = gateOpened,
            RegisteredVehicleId = vehicle?.Id,
            OwnerName = vehicle?.OwnerName,
            OwnerCategory = vehicle?.OwnerCategory
        };

        dbContext.LprReadEvents.Add(lprEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        var eventDto = (await GetLprEventsInternalAsync(1, cancellationToken)).First(e => e.Id == lprEvent.Id);
        RegisteredVehicleDto? vehicleDto = vehicle is null ? null
            : new RegisteredVehicleDto(vehicle.Id, vehicle.Plate, vehicle.Model, vehicle.Color,
                vehicle.OwnerCategory, vehicle.OwnerName, vehicle.ParkingExempt);

        return new LprReadResultDto(gateOpened, message, vehicleDto, eventDto);
    }

    private async Task<TurnstileValidationResultDto> ValidatePatientAppointmentAsync(
        AccessTurnstile turnstile,
        TurnstileValidationRequest request,
        string payload,
        CancellationToken cancellationToken)
    {
        var apptId = ParseGuidPayload(payload, QrAppointmentPrefix);
        if (!apptId.HasValue)
        {
            return new TurnstileValidationResultDto(false, AccessValidationResult.Denied, "QR Code inválido.", null, null, null);
        }

        var appointment = await dbContext.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == apptId.Value && a.IsActive, cancellationToken);

        if (appointment is null || appointment.Status == AppointmentStatus.Cancelled)
        {
            var noAppt = await RecordAccessAsync(
                turnstile, AccessPersonType.Patient, "—", null, null, null, apptId,
                request.Method, request.Direction, AccessValidationResult.NoAppointment,
                "Consulta não encontrada ou cancelada.", cancellationToken);
            return new TurnstileValidationResultDto(false, AccessValidationResult.NoAppointment, "Consulta não encontrada.", null, null, noAppt.Id);
        }

        if (!IsWithinAppointmentWindow(appointment.ScheduledAt))
        {
            var outside = await RecordAccessAsync(
                turnstile, AccessPersonType.Patient, appointment.Patient.FullName,
                appointment.PatientId, null, null, appointment.Id,
                request.Method, request.Direction, AccessValidationResult.OutsideHours,
                "Fora da janela de horário.", cancellationToken);
            return new TurnstileValidationResultDto(false, AccessValidationResult.OutsideHours,
                "Fora do horário permitido para esta consulta.", appointment.Patient.FullName, AccessPersonType.Patient, outside.Id);
        }

        var record = await RecordAccessAsync(
            turnstile, AccessPersonType.Patient, appointment.Patient.FullName,
            appointment.PatientId, null, null, appointment.Id,
            request.Method, request.Direction, AccessValidationResult.Granted,
            $"Consulta {appointment.ScheduledAt:HH:mm}", cancellationToken);

        return new TurnstileValidationResultDto(true, AccessValidationResult.Granted,
            "Acesso liberado — consulta validada.", appointment.Patient.FullName, AccessPersonType.Patient, record.Id);
    }

    private async Task<TurnstileValidationResultDto> ValidateCompanionCredentialAsync(
        AccessTurnstile turnstile,
        TurnstileValidationRequest request,
        string payload,
        CancellationToken cancellationToken)
    {
        var credential = await dbContext.AccessCredentials
            .Include(c => c.AllowedZone)
            .FirstOrDefaultAsync(c => c.IsActive && c.Token == payload, cancellationToken);

        if (credential is null || credential.PersonType != AccessPersonType.Companion)
        {
            return new TurnstileValidationResultDto(false, AccessValidationResult.Denied, "Credencial de acompanhante inválida.", null, null, null);
        }

        return await ValidateCredentialAsync(turnstile, request, credential, cancellationToken);
    }

    private async Task<TurnstileValidationResultDto> ValidateEmployeeAccessAsync(
        AccessTurnstile turnstile,
        TurnstileValidationRequest request,
        string payload,
        CancellationToken cancellationToken)
    {
        var employeeId = ParseGuidPayload(payload, QrEmployeePrefix);
        if (!employeeId.HasValue)
        {
            return new TurnstileValidationResultDto(false, AccessValidationResult.Denied, "Crachá inválido.", null, null, null);
        }

        var employee = await dbContext.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId.Value && e.IsActive, cancellationToken);

        if (employee is null)
        {
            return new TurnstileValidationResultDto(false, AccessValidationResult.Denied, "Colaborador não encontrado.", null, null, null);
        }

        if (turnstile.AccessZone?.RequiresAuthorization == true
            && !IsEmployeeAllowedInZone(employee.Department.Name, turnstile.AccessZone.Code))
        {
            var zoneDenied = await RecordAccessAsync(
                turnstile, MapEmployeeRole(employee.Role), employee.FullName,
                null, employee.Id, null, null,
                request.Method, request.Direction, AccessValidationResult.WrongZone,
                $"Setor restrito: {turnstile.AccessZone.Name}", cancellationToken);
            return new TurnstileValidationResultDto(false, AccessValidationResult.WrongZone,
                $"Sem autorização para {turnstile.AccessZone.Name}.", employee.FullName, MapEmployeeRole(employee.Role), zoneDenied.Id);
        }

        var record = await RecordAccessAsync(
            turnstile, MapEmployeeRole(employee.Role), employee.FullName,
            null, employee.Id, null, null,
            request.Method, request.Direction, AccessValidationResult.Granted,
            employee.Department.Name, cancellationToken);

        return new TurnstileValidationResultDto(true, AccessValidationResult.Granted,
            "Acesso liberado — colaborador.", employee.FullName, MapEmployeeRole(employee.Role), record.Id);
    }

    private async Task<TurnstileValidationResultDto> ValidateCredentialAsync(
        AccessTurnstile turnstile,
        TurnstileValidationRequest request,
        AccessCredential credential,
        CancellationToken cancellationToken)
    {
        if (credential.Status != AccessCredentialStatus.Active)
        {
            return new TurnstileValidationResultDto(false, AccessValidationResult.Expired, "Credencial revogada ou expirada.", credential.HolderName, credential.PersonType, null);
        }

        if (credential.ValidUntil.HasValue && credential.ValidUntil.Value < DateTime.UtcNow)
        {
            credential.Status = AccessCredentialStatus.Expired;
            credential.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return new TurnstileValidationResultDto(false, AccessValidationResult.Expired, "Credencial expirada.", credential.HolderName, credential.PersonType, null);
        }

        var now = TimeOnly.FromDateTime(DateTime.Now);
        if (credential.VisitStartTime.HasValue && now < credential.VisitStartTime.Value)
        {
            return new TurnstileValidationResultDto(false, AccessValidationResult.OutsideHours, "Visita ainda não iniciou.", credential.HolderName, credential.PersonType, null);
        }

        if (credential.VisitEndTime.HasValue && now > credential.VisitEndTime.Value)
        {
            return new TurnstileValidationResultDto(false, AccessValidationResult.OutsideHours, "Horário de visita encerrado.", credential.HolderName, credential.PersonType, null);
        }

        if (credential.AllowedZoneId.HasValue && turnstile.AccessZoneId.HasValue
            && credential.AllowedZoneId != turnstile.AccessZoneId)
        {
            var zoneDenied = await RecordAccessAsync(
                turnstile, credential.PersonType, credential.HolderName,
                credential.PatientId, credential.EmployeeId, credential.VisitorLogId, null,
                request.Method, request.Direction, AccessValidationResult.WrongZone,
                "Setor não autorizado para esta credencial.", cancellationToken);
            return new TurnstileValidationResultDto(false, AccessValidationResult.WrongZone,
                "Setor não autorizado.", credential.HolderName, credential.PersonType, zoneDenied.Id);
        }

        var record = await RecordAccessAsync(
            turnstile, credential.PersonType, credential.HolderName,
            credential.PatientId, credential.EmployeeId, credential.VisitorLogId, null,
            request.Method, request.Direction, AccessValidationResult.Granted,
            "Credencial validada.", cancellationToken);

        return new TurnstileValidationResultDto(true, AccessValidationResult.Granted,
            "Acesso liberado.", credential.HolderName, credential.PersonType, record.Id);
    }

    private async Task<AccessControlRecord> RecordAccessAsync(
        AccessTurnstile turnstile,
        AccessPersonType personType,
        string personName,
        Guid? patientId,
        Guid? employeeId,
        Guid? visitorLogId,
        Guid? appointmentId,
        AccessMethod method,
        AccessDirection direction,
        AccessValidationResult result,
        string details,
        CancellationToken cancellationToken)
    {
        var record = new AccessControlRecord
        {
            PersonType = personType,
            PersonName = personName,
            PatientId = patientId,
            EmployeeId = employeeId,
            VisitorLogId = visitorLogId,
            AppointmentId = appointmentId,
            AccessZoneId = turnstile.AccessZoneId,
            TurnstileId = turnstile.Id,
            Method = method,
            Direction = direction,
            Result = result,
            Location = turnstile.Name,
            Details = details
        };

        dbContext.AccessControlRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (result != AccessValidationResult.Granted)
        {
            dbContext.SecurityIncidents.Add(new SecurityIncident
            {
                Type = SecurityIncidentType.AccessDenied,
                Location = turnstile.Name,
                Description = $"{personName}: {details}",
                ReportedBy = "Sistema — Catraca"
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return record;
    }

    private async Task<KioskTicketDto> IssueKioskTicketInternalAsync(
        KioskTicketType type,
        Guid? patientId,
        string? patientName,
        string? sector,
        Guid? appointmentId,
        CancellationToken cancellationToken)
    {
        var prefix = type switch
        {
            KioskTicketType.Consultation => "CON",
            KioskTicketType.Exam => "EXA",
            KioskTicketType.Hospitalization => "INT",
            KioskTicketType.Emergency => "URG",
            KioskTicketType.Laboratory => "LAB",
            _ => "SEN"
        };

        var countToday = await dbContext.KioskTickets
            .CountAsync(t => t.IsActive && t.TicketType == type && t.IssuedAt.Date == DateTime.UtcNow.Date, cancellationToken);

        var ticket = new KioskTicket
        {
            TicketType = type,
            TicketNumber = $"{prefix}-{countToday + 1:D3}",
            PatientId = patientId,
            PatientName = patientName?.Trim(),
            Sector = sector?.Trim(),
            AppointmentId = appointmentId
        };

        dbContext.KioskTickets.Add(ticket);
        return new KioskTicketDto(ticket.Id, ticket.TicketType, ticket.TicketNumber,
            ticket.PatientName, ticket.Sector, ticket.IssuedAt, ticket.Called);
    }

    private async Task<IReadOnlyList<LprReadEventDto>> GetLprEventsInternalAsync(int take, CancellationToken cancellationToken)
        => await dbContext.LprReadEvents
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.ReadAt)
            .Take(take)
            .Select(e => new LprReadEventDto(
                e.Id, e.Plate, e.CameraLocation, e.Direction, e.GateOpened,
                e.OwnerName, e.OwnerCategory, e.ReadAt))
            .ToListAsync(cancellationToken);

    private async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.AccessZones.AnyAsync(cancellationToken))
        {
            return;
        }

        var zones = new[]
        {
            new AccessZone { Code = "MAIN", Name = "Entrada Principal", Building = "Bloco A", RequiresAuthorization = false },
            new AccessZone { Code = "CC", Name = "Centro Cirúrgico", Building = "Bloco B", Floor = "2", RequiresAuthorization = true },
            new AccessZone { Code = "UTI", Name = "UTI", Building = "Bloco B", Floor = "3", RequiresAuthorization = true },
            new AccessZone { Code = "FARM", Name = "Farmácia", Building = "Bloco A", Floor = "1", RequiresAuthorization = true },
            new AccessZone { Code = "ALMOX", Name = "Almoxarifado", Building = "Subsolo", RequiresAuthorization = true },
            new AccessZone { Code = "AMB", Name = "Ambulatório", Building = "Bloco C", RequiresAuthorization = false },
        };

        dbContext.AccessZones.AddRange(zones);
        await dbContext.SaveChangesAsync(cancellationToken);

        var zoneMap = zones.ToDictionary(z => z.Code, z => z.Id);

        dbContext.AccessTurnstiles.AddRange(
            new AccessTurnstile { Code = "CAT-MAIN-01", Name = "Catraca Entrada Principal", AccessZoneId = zoneMap["MAIN"], IntegrationVendor = "Control iD", IsEntry = true },
            new AccessTurnstile { Code = "CAT-AMB-01", Name = "Catraca Ambulatório", AccessZoneId = zoneMap["AMB"], IntegrationVendor = "Henry Equipamentos", IsEntry = true },
            new AccessTurnstile { Code = "CAT-CC-01", Name = "Catraca Centro Cirúrgico", AccessZoneId = zoneMap["CC"], IntegrationVendor = "Topdata", IsEntry = true },
            new AccessTurnstile { Code = "CAT-UTI-01", Name = "Catraca UTI", AccessZoneId = zoneMap["UTI"], IntegrationVendor = "Control iD", IsEntry = true });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static string BuildAppointmentQr(Guid appointmentId) => $"{QrAppointmentPrefix}{appointmentId:D}";
    public static string BuildEmployeeQr(Guid employeeId) => $"{QrEmployeePrefix}{employeeId:D}";

    private static Guid? ParseGuidPayload(string payload, string prefix)
    {
        var trimmed = payload.Trim();
        if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[prefix.Length..];
        }

        return Guid.TryParse(trimmed, out var id) ? id : null;
    }

    private static bool IsWithinAppointmentWindow(DateTime scheduledAt)
    {
        var now = DateTime.UtcNow;
        return now >= scheduledAt.Subtract(AppointmentEarlyWindow)
            && now <= scheduledAt.Add(AppointmentLateWindow);
    }

    private static string ComputeTemplateHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input.Trim()));
        return Convert.ToHexString(bytes);
    }

    private static string? NormalizePhoto(string? photoData)
    {
        if (string.IsNullOrWhiteSpace(photoData))
        {
            return null;
        }

        if (photoData.Length > 500_000)
        {
            throw new InvalidOperationException("Foto excede o tamanho máximo.");
        }

        return photoData;
    }

    private static string NormalizeCpf(string cpf)
        => cpf.Replace(".", "").Replace("-", "").Trim();

    private static string? MapDepartmentToZone(string department)
        => department.ToLowerInvariant() switch
        {
            var d when d.Contains("farm") => "Farmácia",
            var d when d.Contains("uti") => "UTI",
            var d when d.Contains("cirur") => "Centro Cirúrgico",
            var d when d.Contains("almox") => "Almoxarifado",
            _ => "Entrada Principal"
        };

    private static bool IsEmployeeAllowedInZone(string department, string zoneCode)
    {
        var dept = department.ToLowerInvariant();
        return zoneCode switch
        {
            "CC" => dept.Contains("cirur") || dept.Contains("enferm") || dept.Contains("médic"),
            "UTI" => dept.Contains("uti") || dept.Contains("enferm") || dept.Contains("médic"),
            "FARM" => dept.Contains("farm"),
            "ALMOX" => dept.Contains("almox") || dept.Contains("supri"),
            _ => true
        };
    }

    private static AccessPersonType MapEmployeeRole(EmployeeRole role)
        => role switch
        {
            EmployeeRole.Nurse => AccessPersonType.Nurse,
            _ => AccessPersonType.Employee
        };
}
