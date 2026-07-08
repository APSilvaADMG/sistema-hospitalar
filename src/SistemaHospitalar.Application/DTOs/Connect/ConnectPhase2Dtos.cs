using SistemaHospitalar.Domain.Enums;



namespace SistemaHospitalar.Application.DTOs.Connect;



public record ConnectTicketSummaryDto(

    int TotalAbertos,

    int TotalEmAndamento,

    int TotalAguardando,

    int TotalVencidos);



public record ConnectTicketListItemDto(

    Guid Id,

    string Protocolo,

    string Titulo,

    ConnectTicketCategory Categoria,

    ConnectTicketStatus Status,

    MessagePriority Prioridade,

    string SolicitanteName,

    string? ResponsavelName,

    DateTime? DueAt,

    bool IsOverdue,

    DateTime CreatedAt);



public record ConnectTicketCommentDto(

    Guid Id,

    Guid UserId,

    string UserName,

    string Content,

    DateTime CreatedAt);



public record ConnectTicketDetailDto(

    Guid Id,

    string Protocolo,

    string Titulo,

    string Descricao,

    ConnectTicketCategory Categoria,

    ConnectTicketStatus Status,

    MessagePriority Prioridade,

    Guid SolicitanteId,

    string SolicitanteName,

    Guid? ResponsavelId,

    string? ResponsavelName,

    DateTime? DueAt,

    bool IsOverdue,

    DateTime? ResolvedAt,

    DateTime CreatedAt,

    IReadOnlyList<ConnectTicketCommentDto> Comments);



public record CreateConnectTicketRequest(

    string Titulo,

    string Descricao,

    ConnectTicketCategory Categoria,

    MessagePriority Prioridade,

    Guid? ResponsavelId);



public record UpdateConnectTicketRequest(

    string Titulo,

    string Descricao,

    ConnectTicketCategory Categoria,

    MessagePriority Prioridade);



public record AssignConnectTicketRequest(Guid ResponsavelId);



public record ChangeConnectTicketStatusRequest(ConnectTicketStatus Status);



public record AddConnectTicketCommentRequest(string Content);



public record ConnectTaskSummaryDto(

    int MinhasAbertas,

    int DelegadasAbertas,

    int Vencidas,

    int ConcluidasMes);



public record ConnectTaskListItemDto(

    Guid Id,

    string Titulo,

    ConnectTaskStatus Status,

    MessagePriority Prioridade,

    string CriadorName,

    string? ResponsavelName,

    DateTime? Prazo,

    bool IsOverdue,

    DateTime CreatedAt);



public record ConnectTaskDetailDto(

    Guid Id,

    string Titulo,

    string Descricao,

    ConnectTaskStatus Status,

    MessagePriority Prioridade,

    Guid CriadorId,

    string CriadorName,

    Guid? ResponsavelId,

    string? ResponsavelName,

    DateTime? Prazo,

    bool IsOverdue,

    DateTime CreatedAt);



public record CreateConnectTaskRequest(

    string Titulo,

    string Descricao,

    Guid? ResponsavelId,

    DateTime? Prazo,

    MessagePriority Prioridade);



public record UpdateConnectTaskRequest(

    string Titulo,

    string Descricao,

    Guid? ResponsavelId,

    DateTime? Prazo,

    MessagePriority Prioridade);



public record ChangeConnectTaskStatusRequest(ConnectTaskStatus Status);



public record WorkflowSummaryDto(

    int PendentesParaMim,

    int MinhasPendentes,

    int AprovadasMes,

    int RejeitadasMes);



public record WorkflowStepDto(

    Guid Id,

    int Ordem,

    Guid AprovadorId,

    string AprovadorName,

    WorkflowStepStatus Status,

    string? Justificativa,

    DateTime? RespondedAt);



public record WorkflowInstanceListItemDto(

    Guid Id,

    WorkflowType Tipo,

    string Titulo,

    WorkflowInstanceStatus Status,

    string SolicitanteName,

    DateTime CreatedAt,

    bool PendingForMe);



public record WorkflowInstanceDetailDto(

    Guid Id,

    WorkflowType Tipo,

    string Titulo,

    string Descricao,

    string? Referencia,

    WorkflowInstanceStatus Status,

    Guid SolicitanteId,

    string SolicitanteName,

    DateTime? CompletedAt,

    DateTime CreatedAt,

    IReadOnlyList<WorkflowStepDto> Steps);



public record CreateWorkflowInstanceRequest(

    WorkflowType Tipo,

    string Titulo,

    string Descricao,

    string? Referencia,

    IReadOnlyList<Guid> AprovadorIds);



public record WorkflowDecisionRequest(string? Justificativa);

