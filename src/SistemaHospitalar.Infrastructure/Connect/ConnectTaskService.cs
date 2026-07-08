using Microsoft.EntityFrameworkCore;

using SistemaHospitalar.Application.DTOs.Connect;

using SistemaHospitalar.Application.Interfaces;

using SistemaHospitalar.Domain.Entities;

using SistemaHospitalar.Domain.Enums;

using SistemaHospitalar.Infrastructure.Persistence;



namespace SistemaHospitalar.Infrastructure.Connect;



public class ConnectTaskService(

    AppDbContext db,

    IConnectNotificationService notificationService,

    IConnectRealtimeNotifier realtimeNotifier) : IConnectTaskService

{

    public async Task<ConnectTaskSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)

    {

        var now = DateTime.UtcNow;

        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var openStatuses = new[] { ConnectTaskStatus.Aberta, ConnectTaskStatus.EmAndamento, ConnectTaskStatus.Aguardando };



        var minhasAbertas = await db.ConnectTasks.CountAsync(

            t => t.IsActive && t.DeletedAt == null && t.ResponsavelId == userId && openStatuses.Contains(t.Status),

            cancellationToken);



        var delegadasAbertas = await db.ConnectTasks.CountAsync(

            t => t.IsActive && t.DeletedAt == null && t.CriadorId == userId && t.ResponsavelId != userId

                 && openStatuses.Contains(t.Status),

            cancellationToken);



        var vencidas = await db.ConnectTasks.CountAsync(

            t => t.IsActive && t.DeletedAt == null && t.ResponsavelId == userId

                 && t.Prazo != null && t.Prazo < now && openStatuses.Contains(t.Status),

            cancellationToken);



        var concluidasMes = await db.ConnectTasks.CountAsync(

            t => t.IsActive && t.DeletedAt == null && (t.ResponsavelId == userId || t.CriadorId == userId)

                 && t.Status == ConnectTaskStatus.Concluida && t.UpdatedAt >= monthStart,

            cancellationToken);



        return new ConnectTaskSummaryDto(minhasAbertas, delegadasAbertas, vencidas, concluidasMes);

    }



    public async Task<IReadOnlyList<ConnectTaskListItemDto>> ListAsync(

        Guid userId, string scope, ConnectTaskStatus? status, CancellationToken cancellationToken = default)

    {

        var query = db.ConnectTasks.AsNoTracking()

            .Include(t => t.Criador)

            .Include(t => t.Responsavel)

            .Where(t => t.IsActive && t.DeletedAt == null);



        query = scope.ToLowerInvariant() switch

        {

            "delegated" or "delegadas" => query.Where(t => t.CriadorId == userId && t.ResponsavelId != userId),

            "mine" or "minhas" => query.Where(t => t.ResponsavelId == userId),

            _ => query.Where(t => t.ResponsavelId == userId || t.CriadorId == userId),

        };



        if (status.HasValue) query = query.Where(t => t.Status == status.Value);



        var now = DateTime.UtcNow;

        var items = await query.OrderByDescending(t => t.CreatedAt).Take(200).ToListAsync(cancellationToken);

        return items.Select(t => MapListItem(t, now)).ToList();

    }



    public async Task<ConnectTaskDetailDto?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)

    {

        var task = await LoadTaskAsync(id, cancellationToken);

        if (task is null) return null;

        if (task.ResponsavelId != userId && task.CriadorId != userId) return null;

        return MapDetail(task);

    }



    public async Task<ConnectTaskDetailDto> CreateAsync(

        Guid userId, CreateConnectTaskRequest request, CancellationToken cancellationToken = default)

    {

        var task = new ConnectTask

        {

            Titulo = request.Titulo.Trim(),

            Descricao = request.Descricao.Trim(),

            CriadorId = userId,

            ResponsavelId = request.ResponsavelId ?? userId,

            Prazo = request.Prazo,

            Prioridade = request.Prioridade,

            Status = ConnectTaskStatus.Aberta,

        };



        db.ConnectTasks.Add(task);

        db.CommunicationAuditLogs.Add(new CommunicationAuditLog

        {

            UserId = userId,

            Action = "task.create",

            EntityType = nameof(ConnectTask),

            EntityId = task.Id,

        });



        await db.SaveChangesAsync(cancellationToken);



        if (task.ResponsavelId is Guid assignee && assignee != userId)

        {

            await notificationService.CreateAsync(new CreateConnectNotificationRequest(

                assignee,

                "Nova tarefa atribuída",

                task.Titulo,

                ConnectNotificationCategory.Alert,

                nameof(ConnectTask),

                task.Id), cancellationToken);

        }



        await realtimeNotifier.NotifyTaskUpdatedAsync(task.Id, cancellationToken);

        return MapDetail(await LoadTaskAsync(task.Id, cancellationToken) ?? task);

    }



    public async Task<ConnectTaskDetailDto?> UpdateAsync(

        Guid userId, Guid id, UpdateConnectTaskRequest request, CancellationToken cancellationToken = default)

    {

        var task = await db.ConnectTasks

            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.DeletedAt == null, cancellationToken);



        if (task is null || (task.CriadorId != userId && task.ResponsavelId != userId)) return null;



        var previousAssignee = task.ResponsavelId;

        task.Titulo = request.Titulo.Trim();

        task.Descricao = request.Descricao.Trim();

        task.ResponsavelId = request.ResponsavelId;

        task.Prazo = request.Prazo;

        task.Prioridade = request.Prioridade;

        task.UpdatedAt = DateTime.UtcNow;



        db.CommunicationAuditLogs.Add(new CommunicationAuditLog

        {

            UserId = userId,

            Action = "task.update",

            EntityType = nameof(ConnectTask),

            EntityId = task.Id,

        });



        await db.SaveChangesAsync(cancellationToken);



        if (request.ResponsavelId is Guid newAssignee

            && newAssignee != previousAssignee

            && newAssignee != userId)

        {

            await notificationService.CreateAsync(new CreateConnectNotificationRequest(

                newAssignee,

                "Tarefa atribuída a você",

                task.Titulo,

                ConnectNotificationCategory.Alert,

                nameof(ConnectTask),

                task.Id), cancellationToken);

        }



        await realtimeNotifier.NotifyTaskUpdatedAsync(task.Id, cancellationToken);

        return MapDetail(await LoadTaskAsync(task.Id, cancellationToken) ?? task);

    }



    public async Task<ConnectTaskDetailDto?> ChangeStatusAsync(

        Guid userId, Guid id, ChangeConnectTaskStatusRequest request, CancellationToken cancellationToken = default)

    {

        var task = await db.ConnectTasks

            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.DeletedAt == null, cancellationToken);



        if (task is null || (task.CriadorId != userId && task.ResponsavelId != userId)) return null;



        task.Status = request.Status;

        task.UpdatedAt = DateTime.UtcNow;



        db.CommunicationAuditLogs.Add(new CommunicationAuditLog

        {

            UserId = userId,

            Action = "task.status",

            EntityType = nameof(ConnectTask),

            EntityId = task.Id,

            Details = request.Status.ToString(),

        });



        await db.SaveChangesAsync(cancellationToken);



        var notifyUserId = task.CriadorId == userId ? task.ResponsavelId : task.CriadorId;

        if (notifyUserId is Guid target && target != userId)

        {

            await notificationService.CreateAsync(new CreateConnectNotificationRequest(

                target,

                $"Tarefa atualizada: {task.Titulo}",

                $"Status alterado para {request.Status}.",

                ConnectNotificationCategory.Info,

                nameof(ConnectTask),

                task.Id), cancellationToken);

        }



        await realtimeNotifier.NotifyTaskUpdatedAsync(task.Id, cancellationToken);

        return MapDetail(await LoadTaskAsync(task.Id, cancellationToken) ?? task);

    }



    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)

    {

        var task = await db.ConnectTasks

            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.DeletedAt == null, cancellationToken);



        if (task is null || task.CriadorId != userId) return false;



        task.DeletedAt = DateTime.UtcNow;

        task.UpdatedAt = DateTime.UtcNow;



        db.CommunicationAuditLogs.Add(new CommunicationAuditLog

        {

            UserId = userId,

            Action = "task.delete",

            EntityType = nameof(ConnectTask),

            EntityId = task.Id,

        });



        await db.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyTaskUpdatedAsync(id, cancellationToken);

        return true;

    }



    private async Task<ConnectTask?> LoadTaskAsync(Guid id, CancellationToken cancellationToken)

        => await db.ConnectTasks

            .Include(t => t.Criador)

            .Include(t => t.Responsavel)

            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive && t.DeletedAt == null, cancellationToken);



    private static ConnectTaskListItemDto MapListItem(ConnectTask t, DateTime now)

    {

        var open = t.Status is ConnectTaskStatus.Aberta or ConnectTaskStatus.EmAndamento or ConnectTaskStatus.Aguardando;

        var overdue = open && t.Prazo != null && t.Prazo < now;

        return new ConnectTaskListItemDto(

            t.Id, t.Titulo, t.Status, t.Prioridade,

            t.Criador.FullName, t.Responsavel?.FullName, t.Prazo, overdue, t.CreatedAt);

    }



    private static ConnectTaskDetailDto MapDetail(ConnectTask t)

    {

        var now = DateTime.UtcNow;

        var open = t.Status is ConnectTaskStatus.Aberta or ConnectTaskStatus.EmAndamento or ConnectTaskStatus.Aguardando;

        var overdue = open && t.Prazo != null && t.Prazo < now;

        return new ConnectTaskDetailDto(

            t.Id, t.Titulo, t.Descricao, t.Status, t.Prioridade,

            t.CriadorId, t.Criador.FullName, t.ResponsavelId, t.Responsavel?.FullName,

            t.Prazo, overdue, t.CreatedAt);

    }

}

