using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Application.Serialization;

public static partial class PortugueseEnumRegistry
{
    private static void RegisterComplianceMaps(Dictionary<Type, EnumMap> maps)
    {
        Register(maps,
            (DataSubjectRequestType.Access, "Acesso"),
            (DataSubjectRequestType.Rectification, "Retificação"),
            (DataSubjectRequestType.Portability, "Portabilidade"),
            (DataSubjectRequestType.Anonymization, "Anonimização"),
            (DataSubjectRequestType.Revocation, "Revogação"),
            (DataSubjectRequestType.Erasure, "Eliminação"));

        Register(maps,
            (DataSubjectRequestStatus.Open, "Aberta"),
            (DataSubjectRequestStatus.InReview, "Em análise"),
            (DataSubjectRequestStatus.Completed, "Concluída"),
            (DataSubjectRequestStatus.Rejected, "Rejeitada"));

        Register(maps,
            (PrivacyIncidentSeverity.Low, "Baixa"),
            (PrivacyIncidentSeverity.Medium, "Média"),
            (PrivacyIncidentSeverity.High, "Alta"),
            (PrivacyIncidentSeverity.Critical, "Crítica"));

        Register(maps,
            (PrivacyIncidentStatus.Detected, "Detectado"),
            (PrivacyIncidentStatus.Investigating, "Em investigação"),
            (PrivacyIncidentStatus.Contained, "Contido"),
            (PrivacyIncidentStatus.Notified, "Notificado"),
            (PrivacyIncidentStatus.Closed, "Encerrado"));
    }
}
