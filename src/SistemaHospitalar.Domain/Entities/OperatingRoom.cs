using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class OperatingRoom : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public OperatingRoomStatus Status { get; set; } = OperatingRoomStatus.Available;
    public string? Location { get; set; }

    public ICollection<Surgery> Surgeries { get; set; } = [];
}
