using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Parking;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class ParkingService(AppDbContext dbContext) : IParkingService
{
    public const string QrPrefix = "HMS-PARK:";

    public async Task<IReadOnlyList<ParkingZoneDto>> GetZonesAsync(CancellationToken cancellationToken = default)
    {
        var zones = await dbContext.ParkingZones
            .AsNoTracking()
            .Where(z => z.IsActive)
            .OrderBy(z => z.Name)
            .ToListAsync(cancellationToken);

        var result = new List<ParkingZoneDto>();
        foreach (var z in zones)
        {
            var occupied = await dbContext.ParkingSessions
                .CountAsync(s => s.ParkingZoneId == z.Id && s.Status == ParkingSessionStatus.Active, cancellationToken);
            result.Add(new ParkingZoneDto(z.Id, z.Name, z.TotalSpots, occupied, z.HourlyRate, z.Description));
        }

        return result;
    }

    public async Task<IReadOnlyList<ParkingSessionDto>> GetSessionsAsync(
        bool? activeOnly, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ParkingSessions.AsNoTracking().Where(s => s.IsActive);

        if (activeOnly == true)
        {
            query = query.Where(s => s.Status == ParkingSessionStatus.Active);
        }

        var sessions = await query
            .OrderByDescending(s => s.EnteredAt)
            .Take(100)
            .Include(s => s.ParkingZone)
            .Include(s => s.Patient)
            .ToListAsync(cancellationToken);

        return sessions.Select(MapSession).ToList();
    }

    public async Task<ParkingSessionDto> CheckInAsync(
        CheckInParkingRequest request, CancellationToken cancellationToken = default)
    {
        var zone = await dbContext.ParkingZones
            .FirstOrDefaultAsync(z => z.Id == request.ZoneId && z.IsActive, cancellationToken);

        if (zone is null)
        {
            throw new InvalidOperationException("Zona não encontrada.");
        }

        var occupied = await dbContext.ParkingSessions
            .CountAsync(s => s.ParkingZoneId == zone.Id && s.Status == ParkingSessionStatus.Active, cancellationToken);

        if (occupied >= zone.TotalSpots)
        {
            throw new InvalidOperationException("Estacionamento lotado.");
        }

        var plate = request.VehiclePlate.Trim().ToUpperInvariant();
        var activeSamePlate = await dbContext.ParkingSessions
            .AnyAsync(s => s.VehiclePlate == plate && s.Status == ParkingSessionStatus.Active, cancellationToken);

        if (activeSamePlate)
        {
            throw new InvalidOperationException("Veículo já possui sessão ativa.");
        }

        var session = new ParkingSession
        {
            ParkingZoneId = zone.Id,
            VehiclePlate = plate,
            PatientId = request.PatientId
        };

        dbContext.ParkingSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetSessionByIdAsync(session.Id, cancellationToken))!;
    }

    public async Task<ParkingSessionDto?> PaySessionAsync(
        PayParkingRequest request, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.ParkingSessions
            .Include(s => s.ParkingZone)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.IsActive, cancellationToken);

        if (session is null || session.Status != ParkingSessionStatus.Active)
        {
            return null;
        }

        if (session.IsPaid)
        {
            throw new InvalidOperationException("Estacionamento já foi pago.");
        }

        session.AmountCharged = CalculateAmount(session.EnteredAt, DateTime.UtcNow, session.ParkingZone.HourlyRate);
        session.IsPaid = true;
        session.PaidAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetSessionByIdAsync(session.Id, cancellationToken);
    }

    public async Task<ParkingSessionDto?> CheckOutAsync(
        CheckOutParkingRequest request, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.ParkingSessions
            .Include(s => s.ParkingZone)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.IsActive, cancellationToken);

        if (session is null || session.Status != ParkingSessionStatus.Active)
        {
            return null;
        }

        if (!session.IsPaid)
        {
            throw new InvalidOperationException("Pagamento pendente. Registre o pagamento antes de liberar a saída.");
        }

        return await CompleteExitAsync(session, cancellationToken);
    }

    public async Task<ParkingGateExitResultDto> ProcessGateExitAsync(
        ParkingGateExitRequest request, CancellationToken cancellationToken = default)
    {
        var sessionId = ParseQrPayload(request.QrPayload);
        if (sessionId is null)
        {
            return new ParkingGateExitResultDto(false, "QR Code inválido ou ticket não reconhecido.", null);
        }

        var session = await dbContext.ParkingSessions
            .Include(s => s.ParkingZone)
            .FirstOrDefaultAsync(s => s.Id == sessionId.Value && s.IsActive, cancellationToken);

        if (session is null)
        {
            return new ParkingGateExitResultDto(false, "Ticket não encontrado.", null);
        }

        if (session.Status != ParkingSessionStatus.Active)
        {
            return new ParkingGateExitResultDto(false, "Este ticket já foi utilizado na saída.", MapSession(session));
        }

        if (!session.IsPaid)
        {
            var estimated = CalculateAmount(session.EnteredAt, DateTime.UtcNow, session.ParkingZone.HourlyRate);
            return new ParkingGateExitResultDto(
                false,
                $"Pagamento pendente ({estimated:C}). Dirija-se ao caixa antes de sair.",
                MapSession(session));
        }

        var completed = await CompleteExitAsync(session, cancellationToken);
        return new ParkingGateExitResultDto(
            true,
            $"Saída liberada — placa {session.VehiclePlate}. Boa viagem!",
            completed);
    }

    private async Task<ParkingSessionDto> CompleteExitAsync(
        ParkingSession session, CancellationToken cancellationToken)
    {
        session.ExitedAt = DateTime.UtcNow;
        session.Status = ParkingSessionStatus.Completed;
        session.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetSessionByIdAsync(session.Id, cancellationToken))!;
    }

    private async Task<ParkingSessionDto?> GetSessionByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var session = await dbContext.ParkingSessions
            .AsNoTracking()
            .Include(s => s.ParkingZone)
            .Include(s => s.Patient)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return session is null ? null : MapSession(session);
    }

    private static ParkingSessionDto MapSession(ParkingSession s)
    {
        decimal? estimated = null;
        if (s.Status == ParkingSessionStatus.Active && !s.IsPaid)
        {
            estimated = CalculateAmount(s.EnteredAt, DateTime.UtcNow, s.ParkingZone.HourlyRate);
        }

        return new ParkingSessionDto(
            s.Id,
            s.ParkingZoneId,
            s.ParkingZone.Name,
            s.VehiclePlate,
            s.Patient?.FullName,
            s.EnteredAt,
            s.ExitedAt,
            s.Status,
            s.AmountCharged,
            s.IsPaid,
            s.PaidAt,
            estimated,
            BuildQrPayload(s.Id));
    }

    public static string BuildQrPayload(Guid sessionId) => $"{QrPrefix}{sessionId:D}";

    public static Guid? ParseQrPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        var trimmed = payload.Trim();
        if (trimmed.StartsWith(QrPrefix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[QrPrefix.Length..];
        }

        return Guid.TryParse(trimmed, out var id) ? id : null;
    }

    private static decimal CalculateAmount(DateTime enteredAt, DateTime referenceTime, decimal hourlyRate)
    {
        var hours = Math.Max(1, Math.Ceiling((referenceTime - enteredAt).TotalHours));
        return (decimal)hours * hourlyRate;
    }
}
