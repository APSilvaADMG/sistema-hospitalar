using SistemaHospitalar.Application.DTOs.Connect;

using SistemaHospitalar.Domain.Enums;



namespace SistemaHospitalar.Application.Interfaces;



public interface IConnectTicketService

{

    Task<ConnectTicketSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConnectTicketListItemDto>> ListAsync(

        Guid userId,

        ConnectTicketStatus? status,

        ConnectTicketCategory? category,

        MessagePriority? priority,

        bool? assignedToMe,

        bool? myRequests,

        string? search,

        CancellationToken cancellationToken = default);

    Task<ConnectTicketDetailDto?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    Task<ConnectTicketDetailDto> CreateAsync(Guid userId, CreateConnectTicketRequest request, CancellationToken cancellationToken = default);

    Task<ConnectTicketDetailDto?> UpdateAsync(Guid userId, Guid id, UpdateConnectTicketRequest request, CancellationToken cancellationToken = default);

    Task<ConnectTicketDetailDto?> AssignAsync(Guid userId, Guid id, AssignConnectTicketRequest request, CancellationToken cancellationToken = default);

    Task<ConnectTicketDetailDto?> ChangeStatusAsync(Guid userId, Guid id, ChangeConnectTicketStatusRequest request, CancellationToken cancellationToken = default);

    Task<ConnectTicketCommentDto?> AddCommentAsync(Guid userId, Guid id, AddConnectTicketCommentRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

}



public interface IConnectTaskService

{

    Task<ConnectTaskSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConnectTaskListItemDto>> ListAsync(

        Guid userId,

        string scope,

        ConnectTaskStatus? status,

        CancellationToken cancellationToken = default);

    Task<ConnectTaskDetailDto?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    Task<ConnectTaskDetailDto> CreateAsync(Guid userId, CreateConnectTaskRequest request, CancellationToken cancellationToken = default);

    Task<ConnectTaskDetailDto?> UpdateAsync(Guid userId, Guid id, UpdateConnectTaskRequest request, CancellationToken cancellationToken = default);

    Task<ConnectTaskDetailDto?> ChangeStatusAsync(Guid userId, Guid id, ChangeConnectTaskStatusRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

}



public interface IConnectWorkflowService

{

    Task<WorkflowSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowInstanceListItemDto>> ListAsync(

        Guid userId,

        bool? pendingForMe,

        CancellationToken cancellationToken = default);

    Task<WorkflowInstanceDetailDto?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    Task<WorkflowInstanceDetailDto> CreateAsync(Guid userId, CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default);

    Task<WorkflowInstanceDetailDto?> ApproveAsync(Guid userId, Guid id, WorkflowDecisionRequest request, CancellationToken cancellationToken = default);

    Task<WorkflowInstanceDetailDto?> RejectAsync(Guid userId, Guid id, WorkflowDecisionRequest request, CancellationToken cancellationToken = default);

}

