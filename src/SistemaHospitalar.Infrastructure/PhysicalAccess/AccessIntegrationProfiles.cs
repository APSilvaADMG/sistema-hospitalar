using SistemaHospitalar.Application.DTOs.PhysicalAccess;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.PhysicalAccess;

public static class AccessIntegrationProfiles
{
    public static IReadOnlyList<AccessIntegrationProfileDto> All { get; } =
    [
        new("Control iD", AccessIntegrationCategory.Turnstile, "Catracas e leitores RFID — integração mock via API REST", true, "https://mock.controlid.local/api/v1"),
        new("Henry Equipamentos", AccessIntegrationCategory.Turnstile, "Catracas Henry — protocolo proprietário (mock)", true, null),
        new("Topdata", AccessIntegrationCategory.Turnstile, "Catracas Topdata — controle de acesso físico (mock)", true, null),
        new("Control iD", AccessIntegrationCategory.FacialRecognition, "Reconhecimento facial iDFace — template biométrico", true, "https://mock.controlid.local/biometric"),
        new("Intelbras", AccessIntegrationCategory.FacialRecognition, "Câmeras e terminais faciais Intelbras (mock)", true, "https://mock.intelbras.local/face"),
        new("Pumatronix", AccessIntegrationCategory.Parking, "OCR/LPR e cancelas Pumatronix (mock)", true, "https://mock.pumatronix.local/lpr"),
        new("Veek", AccessIntegrationCategory.Parking, "Gestão de estacionamento Veek (mock)", true, "https://mock.veek.local/parking"),
    ];
}
