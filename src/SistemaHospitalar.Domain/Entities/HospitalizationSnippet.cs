using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class HospitalizationSnippet : BaseEntity
{
    public HospitalizationSnippetType Type { get; set; }
    public string Text { get; set; } = string.Empty;
    public string NormalizedText { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public Guid? CreatedByUserId { get; set; }
}
