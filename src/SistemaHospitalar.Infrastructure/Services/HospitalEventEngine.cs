using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Application.DTOs.Events;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Messaging;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class HospitalEventEngine(
    AppDbContext dbContext,
    IHotelariaHospitalarService hotelariaHospitalarService,
    ITransportService transportService,
    IPendencyService pendencyService,
    HospitalEventPublisher eventPublisher,
    ILogger<HospitalEventEngine> logger) : IHospitalEventEngine
{
    public async Task<HospitalEventLogDto> PublishAndProcessAsync(
        string eventType,
        object payload,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default)
    {
        var routingKey = eventType;
        var payloadJson = JsonSerializer.Serialize(payload);

        var log = new HospitalEventLog
        {
            EventType = eventType,
            RoutingKey = routingKey,
            PayloadJson = payloadJson,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            Status = HospitalEventLogStatus.Pending,
        };

        dbContext.HospitalEventLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await RunHandlersAsync(eventType, payload, cancellationToken);
            await eventPublisher.PublishAsync(routingKey, payload, cancellationToken);

            log.Status = HospitalEventLogStatus.Processed;
            log.ProcessedAt = DateTime.UtcNow;
            log.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao processar evento {EventType}", eventType);
            log.Status = HospitalEventLogStatus.Failed;
            log.ErrorMessage = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            log.ProcessedAt = DateTime.UtcNow;
            log.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToDto(log);
    }

    public async Task<IReadOnlyList<HospitalEventLogDto>> GetRecentAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.HospitalEventLogs
            .AsNoTracking()
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(e => new HospitalEventLogDto(
                e.Id,
                e.EventType,
                e.RoutingKey,
                e.Status,
                e.RelatedEntityId,
                e.RelatedEntityType,
                e.CreatedAt,
                e.ProcessedAt,
                e.ErrorMessage))
            .ToListAsync(cancellationToken);
    }

    private async Task RunHandlersAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        switch (eventType)
        {
            case HospitalEventTypes.PatientDischarged:
                await HandlePatientDischargedAsync(payload, cancellationToken);
                break;
            case HospitalEventTypes.PrescriptionSigned:
                await HandlePrescriptionSignedAsync(payload, cancellationToken);
                break;
            case HospitalEventTypes.StockLow:
                await HandleStockLowAsync(payload, cancellationToken);
                break;
        }
    }

    private async Task HandlePatientDischargedAsync(object payload, CancellationToken cancellationToken)
    {
        var doc = JsonSerializer.SerializeToDocument(payload);
        var root = doc.RootElement;

        if (!root.TryGetProperty("bedId", out var bedIdEl) || !Guid.TryParse(bedIdEl.GetString(), out var bedId))
        {
            return;
        }

        Guid? hospitalizationId = root.TryGetProperty("hospitalizationId", out var hospEl)
            && Guid.TryParse(hospEl.GetString(), out var hid)
            ? hid
            : null;

        var patientName = root.TryGetProperty("patientName", out var nameEl)
            ? nameEl.GetString() ?? "Paciente"
            : "Paciente";

        var wardName = root.TryGetProperty("wardName", out var wardEl)
            ? wardEl.GetString() ?? "Internação"
            : "Internação";

        var bedNumber = root.TryGetProperty("bedNumber", out var bedNumEl)
            ? bedNumEl.GetString() ?? "—"
            : "—";

        await hotelariaHospitalarService.RequestBedCleaningAsync(
            bedId,
            hospitalizationId,
            CleaningType.Terminal,
            CleaningTriggerReason.Discharge,
            cancellationToken);

        var hospitalityUser = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role == UserRole.Hospitality)
            .OrderBy(u => u.CreatedAt)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (hospitalityUser != Guid.Empty)
        {
            await UpsertPendencyAsync(
                hospitalityUser,
                PendingItemType.BedCleaning,
                PendingItemPriority.Alta,
                PendingModule.Hotelaria,
                $"Higienização terminal — Leito {bedNumber}",
                $"Alta hospitalar em {wardName}. Aguardando higienização terminal do leito {bedNumber}.",
                "/hotelaria",
                hospitalizationId,
                "Hospitalization",
                cancellationToken);
        }

        if (root.TryGetProperty("requestTransport", out var transportEl)
            && transportEl.ValueKind == JsonValueKind.True)
        {
            await transportService.CreateRequestAsync(
                new Application.DTOs.Transport.CreateTransportRequestRequest(
                    root.TryGetProperty("patientId", out var pidEl) && Guid.TryParse(pidEl.GetString(), out var pid) ? pid : null,
                    hospitalizationId,
                    patientName,
                    TransportLocationType.Hospitalization,
                    $"{wardName} · Leito {bedNumber}",
                    TransportLocationType.Discharge,
                    "Portaria / Saída",
                    TransportPriority.Normal,
                    "Transporte pós-alta (evento automático)"),
                "event-engine",
                cancellationToken);
        }
    }

    private async Task HandlePrescriptionSignedAsync(object payload, CancellationToken cancellationToken)
    {
        var doc = JsonSerializer.SerializeToDocument(payload);
        var root = doc.RootElement;

        if (!root.TryGetProperty("requiresPharmacyReview", out var reviewEl) || !reviewEl.GetBoolean())
        {
            return;
        }

        var pharmacyUser = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role == UserRole.Pharmacy)
            .OrderBy(u => u.CreatedAt)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (pharmacyUser == Guid.Empty)
        {
            return;
        }

        var entryId = root.TryGetProperty("entryId", out var idEl) && Guid.TryParse(idEl.GetString(), out var eid)
            ? eid
            : (Guid?)null;

        await UpsertPendencyAsync(
            pharmacyUser,
            PendingItemType.WorkflowPending,
            PendingItemPriority.Normal,
            PendingModule.System,
            "Prescrição assinada — revisão farmácia",
            "Nova prescrição assinada requer conferência farmacêutica.",
            "/farmacia",
            entryId,
            "MedicalRecordEntry",
            cancellationToken);
    }

    private async Task HandleStockLowAsync(object payload, CancellationToken cancellationToken)
    {
        var doc = JsonSerializer.SerializeToDocument(payload);
        var root = doc.RootElement;

        var productName = root.TryGetProperty("productName", out var nameEl)
            ? nameEl.GetString() ?? "Produto"
            : "Produto";

        var productId = root.TryGetProperty("productId", out var idEl) && Guid.TryParse(idEl.GetString(), out var pid)
            ? pid
            : (Guid?)null;

        var warehouseUser = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role == UserRole.Warehouse)
            .OrderBy(u => u.CreatedAt)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (warehouseUser == Guid.Empty)
        {
            return;
        }

        await UpsertPendencyAsync(
            warehouseUser,
            PendingItemType.LowStock,
            PendingItemPriority.Alta,
            PendingModule.Inventory,
            $"Estoque baixo: {productName}",
            $"Saldo abaixo do mínimo para {productName}. Solicitar reposição.",
            "/estoque/dashboard",
            productId,
            "Product",
            cancellationToken);
    }

    private async Task UpsertPendencyAsync(
        Guid userId,
        PendingItemType tipo,
        PendingItemPriority prioridade,
        PendingModule modulo,
        string titulo,
        string descricao,
        string link,
        Guid? sourceId,
        string? sourceType,
        CancellationToken cancellationToken)
    {
        var existing = sourceId.HasValue
            ? await dbContext.PendingItems.FirstOrDefaultAsync(
                p => p.IsActive
                    && p.UsuarioResponsavelId == userId
                    && p.Tipo == tipo
                    && p.SourceEntityId == sourceId,
                cancellationToken)
            : null;

        if (existing is null)
        {
            dbContext.PendingItems.Add(new PendingItem
            {
                UsuarioResponsavelId = userId,
                Titulo = titulo,
                Descricao = descricao,
                Modulo = modulo,
                Tipo = tipo,
                Status = PendingItemStatus.Aberta,
                Prioridade = prioridade,
                LinkDestino = link,
                SourceEntityId = sourceId,
                SourceEntityType = sourceType,
                DataAbertura = DateTime.UtcNow,
            });
        }
        else if (existing.Status == PendingItemStatus.Concluida)
        {
            existing.Status = PendingItemStatus.Aberta;
            existing.DataAbertura = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await pendencyService.SyncForUserAsync(userId, cancellationToken);
    }

    private static HospitalEventLogDto ToDto(HospitalEventLog log) => new(
        log.Id,
        log.EventType,
        log.RoutingKey,
        log.Status,
        log.RelatedEntityId,
        log.RelatedEntityType,
        log.CreatedAt,
        log.ProcessedAt,
        log.ErrorMessage);
}
