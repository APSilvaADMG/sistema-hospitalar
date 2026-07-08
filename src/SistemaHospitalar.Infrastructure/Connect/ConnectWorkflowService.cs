using Microsoft.EntityFrameworkCore;

using SistemaHospitalar.Application.DTOs.Connect;

using SistemaHospitalar.Application.Interfaces;

using SistemaHospitalar.Domain.Entities;

using SistemaHospitalar.Domain.Enums;

using SistemaHospitalar.Infrastructure.Persistence;



namespace SistemaHospitalar.Infrastructure.Connect;



public class ConnectWorkflowService(

    AppDbContext db,

    IConnectNotificationService notificationService) : IConnectWorkflowService

{

    public async Task<WorkflowSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)

    {

        var now = DateTime.UtcNow;

        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);



        var pendentesParaMim = await db.WorkflowSteps.CountAsync(

            s => s.IsActive && s.AprovadorId == userId && s.Status == WorkflowStepStatus.Pendente

                 && s.Instance.IsActive && s.Instance.DeletedAt == null

                 && s.Instance.Status == WorkflowInstanceStatus.Pendente,

            cancellationToken);



        var minhasPendentes = await db.WorkflowInstances.CountAsync(

            i => i.IsActive && i.DeletedAt == null && i.SolicitanteId == userId

                 && i.Status == WorkflowInstanceStatus.Pendente,

            cancellationToken);



        var aprovadasMes = await db.WorkflowInstances.CountAsync(

            i => i.IsActive && i.DeletedAt == null

                 && (i.SolicitanteId == userId || i.Steps.Any(s => s.AprovadorId == userId))

                 && i.Status == WorkflowInstanceStatus.Aprovado && i.CompletedAt >= monthStart,

            cancellationToken);



        var rejeitadasMes = await db.WorkflowInstances.CountAsync(

            i => i.IsActive && i.DeletedAt == null

                 && (i.SolicitanteId == userId || i.Steps.Any(s => s.AprovadorId == userId))

                 && i.Status == WorkflowInstanceStatus.Rejeitado && i.CompletedAt >= monthStart,

            cancellationToken);



        return new WorkflowSummaryDto(pendentesParaMim, minhasPendentes, aprovadasMes, rejeitadasMes);

    }



    public async Task<IReadOnlyList<WorkflowInstanceListItemDto>> ListAsync(

        Guid userId, bool? pendingForMe, CancellationToken cancellationToken = default)

    {

        var query = db.WorkflowInstances.AsNoTracking()

            .Include(i => i.Solicitante)

            .Include(i => i.Steps)

            .Where(i => i.IsActive && i.DeletedAt == null);



        if (pendingForMe == true)

        {

            query = query.Where(i => i.Status == WorkflowInstanceStatus.Pendente

                && i.Steps.Any(s => s.AprovadorId == userId && s.Status == WorkflowStepStatus.Pendente));

        }

        else

        {

            query = query.Where(i => i.SolicitanteId == userId

                || i.Steps.Any(s => s.AprovadorId == userId));

        }



        var items = await query.OrderByDescending(i => i.CreatedAt).Take(200).ToListAsync(cancellationToken);



        return items.Select(i =>

        {

            var pendingForUser = i.Status == WorkflowInstanceStatus.Pendente

                && i.Steps.Any(s => s.AprovadorId == userId && s.Status == WorkflowStepStatus.Pendente);

            return new WorkflowInstanceListItemDto(

                i.Id, i.Tipo, i.Titulo, i.Status, i.Solicitante.FullName, i.CreatedAt, pendingForUser);

        }).ToList();

    }



    public async Task<WorkflowInstanceDetailDto?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)

    {

        var instance = await LoadInstanceAsync(id, cancellationToken);

        if (instance is null) return null;

        if (instance.SolicitanteId != userId && instance.Steps.All(s => s.AprovadorId != userId)) return null;

        return MapDetail(instance);

    }



    public async Task<WorkflowInstanceDetailDto> CreateAsync(

        Guid userId, CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default)

    {

        if (request.AprovadorIds.Count == 0)

            throw new InvalidOperationException("Informe ao menos um aprovador.");



        var instance = new WorkflowInstance

        {

            Tipo = request.Tipo,

            Titulo = request.Titulo.Trim(),

            Descricao = request.Descricao.Trim(),

            Referencia = request.Referencia?.Trim(),

            SolicitanteId = userId,

            Status = WorkflowInstanceStatus.Pendente,

        };



        db.WorkflowInstances.Add(instance);



        var ordem = 1;

        foreach (var aprovadorId in request.AprovadorIds.Distinct())

        {

            db.WorkflowSteps.Add(new WorkflowStep

            {

                InstanceId = instance.Id,

                Ordem = ordem++,

                AprovadorId = aprovadorId,

                Status = WorkflowStepStatus.Pendente,

            });

        }



        db.CommunicationAuditLogs.Add(new CommunicationAuditLog

        {

            UserId = userId,

            Action = "workflow.create",

            EntityType = nameof(WorkflowInstance),

            EntityId = instance.Id,

            Details = request.Tipo.ToString(),

        });



        await db.SaveChangesAsync(cancellationToken);



        var firstApprover = request.AprovadorIds.First();

        await notificationService.CreateAsync(new CreateConnectNotificationRequest(

            firstApprover,

            "Aprovação pendente",

            request.Titulo,

            ConnectNotificationCategory.System,

            nameof(WorkflowInstance),

            instance.Id), cancellationToken);



        return MapDetail(await LoadInstanceAsync(instance.Id, cancellationToken) ?? instance);

    }



    public async Task<WorkflowInstanceDetailDto?> ApproveAsync(

        Guid userId, Guid id, WorkflowDecisionRequest request, CancellationToken cancellationToken = default)

        => await DecideAsync(userId, id, approve: true, request, cancellationToken);



    public async Task<WorkflowInstanceDetailDto?> RejectAsync(

        Guid userId, Guid id, WorkflowDecisionRequest request, CancellationToken cancellationToken = default)

        => await DecideAsync(userId, id, approve: false, request, cancellationToken);



    private async Task<WorkflowInstanceDetailDto?> DecideAsync(

        Guid userId, Guid id, bool approve, WorkflowDecisionRequest request, CancellationToken cancellationToken)

    {

        var instance = await db.WorkflowInstances

            .Include(i => i.Steps).ThenInclude(s => s.Aprovador)

            .Include(i => i.Solicitante)

            .FirstOrDefaultAsync(i => i.Id == id && i.IsActive && i.DeletedAt == null, cancellationToken);



        if (instance is null || instance.Status != WorkflowInstanceStatus.Pendente) return null;



        var currentStep = instance.Steps

            .Where(s => s.IsActive && s.Status == WorkflowStepStatus.Pendente)

            .OrderBy(s => s.Ordem)

            .FirstOrDefault();



        if (currentStep is null || currentStep.AprovadorId != userId) return null;



        currentStep.Status = approve ? WorkflowStepStatus.Aprovado : WorkflowStepStatus.Rejeitado;

        currentStep.Justificativa = request.Justificativa?.Trim();

        currentStep.RespondedAt = DateTime.UtcNow;

        currentStep.UpdatedAt = DateTime.UtcNow;



        db.CommunicationAuditLogs.Add(new CommunicationAuditLog

        {

            UserId = userId,

            Action = approve ? "workflow.approve" : "workflow.reject",

            EntityType = nameof(WorkflowInstance),

            EntityId = instance.Id,

            Details = request.Justificativa,

        });



        if (!approve)

        {

            instance.Status = WorkflowInstanceStatus.Rejeitado;

            instance.CompletedAt = DateTime.UtcNow;

            instance.UpdatedAt = DateTime.UtcNow;



            await db.SaveChangesAsync(cancellationToken);



            await notificationService.CreateAsync(new CreateConnectNotificationRequest(

                instance.SolicitanteId,

                $"Aprovação rejeitada: {instance.Titulo}",

                request.Justificativa ?? "Sem justificativa informada.",

                ConnectNotificationCategory.Alert,

                nameof(WorkflowInstance),

                instance.Id), cancellationToken);



            return MapDetail(instance);

        }



        var nextStep = instance.Steps

            .Where(s => s.IsActive && s.Status == WorkflowStepStatus.Pendente && s.Id != currentStep.Id)

            .OrderBy(s => s.Ordem)

            .FirstOrDefault();



        if (nextStep is null)

        {

            instance.Status = WorkflowInstanceStatus.Aprovado;

            instance.CompletedAt = DateTime.UtcNow;

            instance.UpdatedAt = DateTime.UtcNow;



            await db.SaveChangesAsync(cancellationToken);



            await notificationService.CreateAsync(new CreateConnectNotificationRequest(

                instance.SolicitanteId,

                $"Aprovação concluída: {instance.Titulo}",

                "Todas as etapas foram aprovadas.",

                ConnectNotificationCategory.Info,

                nameof(WorkflowInstance),

                instance.Id), cancellationToken);

        }

        else

        {

            await db.SaveChangesAsync(cancellationToken);



            await notificationService.CreateAsync(new CreateConnectNotificationRequest(

                nextStep.AprovadorId,

                "Aprovação pendente",

                instance.Titulo,

                ConnectNotificationCategory.System,

                nameof(WorkflowInstance),

                instance.Id), cancellationToken);

        }



        return MapDetail(await LoadInstanceAsync(instance.Id, cancellationToken) ?? instance);

    }



    private async Task<WorkflowInstance?> LoadInstanceAsync(Guid id, CancellationToken cancellationToken)

        => await db.WorkflowInstances

            .Include(i => i.Solicitante)

            .Include(i => i.Steps).ThenInclude(s => s.Aprovador)

            .FirstOrDefaultAsync(i => i.Id == id && i.IsActive && i.DeletedAt == null, cancellationToken);



    private static WorkflowInstanceDetailDto MapDetail(WorkflowInstance i)

    {

        var steps = i.Steps.Where(s => s.IsActive).OrderBy(s => s.Ordem)

            .Select(s => new WorkflowStepDto(

                s.Id, s.Ordem, s.AprovadorId, s.Aprovador.FullName,

                s.Status, s.Justificativa, s.RespondedAt))

            .ToList();



        return new WorkflowInstanceDetailDto(

            i.Id, i.Tipo, i.Titulo, i.Descricao, i.Referencia, i.Status,

            i.SolicitanteId, i.Solicitante.FullName, i.CompletedAt, i.CreatedAt, steps);

    }

}

