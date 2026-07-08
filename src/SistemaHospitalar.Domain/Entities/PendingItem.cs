using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class PendingItem : BaseEntity
{
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public PendingModule Modulo { get; set; } = PendingModule.System;
    public PendingItemType Tipo { get; set; } = PendingItemType.SystemAlert;
    public PendingItemStatus Status { get; set; } = PendingItemStatus.Aberta;
    public PendingItemPriority Prioridade { get; set; } = PendingItemPriority.Normal;
    public string? Responsavel { get; set; }
    public string? Setor { get; set; }
    public DateTime DataAbertura { get; set; } = DateTime.UtcNow;
    public DateTime? DataLimite { get; set; }
    public string? LinkDestino { get; set; }
    public Guid UsuarioResponsavelId { get; set; }
    public User UsuarioResponsavel { get; set; } = null!;
    public string? SourceEntityType { get; set; }
    public Guid? SourceEntityId { get; set; }
}
