using SistemaHospitalar.Domain.Common;

using SistemaHospitalar.Domain.Enums;



namespace SistemaHospitalar.Domain.Entities;



public class ConnectTicket : BaseEntity

{

    public string Protocolo { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public ConnectTicketCategory Categoria { get; set; }

    public Guid SolicitanteId { get; set; }

    public User Solicitante { get; set; } = null!;

    public Guid? ResponsavelId { get; set; }

    public User? Responsavel { get; set; }

    public MessagePriority Prioridade { get; set; } = MessagePriority.Normal;

    public ConnectTicketStatus Status { get; set; } = ConnectTicketStatus.Aberto;

    public DateTime? DueAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? LastSlaAlertAt { get; set; }

    public DateTime? DeletedAt { get; set; }



    public ICollection<ConnectTicketComment> Comments { get; set; } = [];

}



public class ConnectTicketComment : BaseEntity

{

    public Guid TicketId { get; set; }

    public ConnectTicket Ticket { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

}



public class ConnectTask : BaseEntity

{

    public string Titulo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public Guid CriadorId { get; set; }

    public User Criador { get; set; } = null!;

    public Guid? ResponsavelId { get; set; }

    public User? Responsavel { get; set; }

    public DateTime? Prazo { get; set; }

    public MessagePriority Prioridade { get; set; } = MessagePriority.Normal;

    public ConnectTaskStatus Status { get; set; } = ConnectTaskStatus.Aberta;

    public DateTime? DeletedAt { get; set; }

}



public class WorkflowInstance : BaseEntity

{

    public WorkflowType Tipo { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public string? Referencia { get; set; }

    public Guid SolicitanteId { get; set; }

    public User Solicitante { get; set; } = null!;

    public WorkflowInstanceStatus Status { get; set; } = WorkflowInstanceStatus.Pendente;

    public DateTime? CompletedAt { get; set; }

    public DateTime? DeletedAt { get; set; }



    public ICollection<WorkflowStep> Steps { get; set; } = [];

}



public class WorkflowStep : BaseEntity

{

    public Guid InstanceId { get; set; }

    public WorkflowInstance Instance { get; set; } = null!;

    public int Ordem { get; set; }

    public Guid AprovadorId { get; set; }

    public User Aprovador { get; set; } = null!;

    public WorkflowStepStatus Status { get; set; } = WorkflowStepStatus.Pendente;

    public string? Justificativa { get; set; }

    public DateTime? RespondedAt { get; set; }

}

