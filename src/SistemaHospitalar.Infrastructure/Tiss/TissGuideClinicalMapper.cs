using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Tiss;

internal static class TissGuideClinicalMapper
{
    public static TissGuideClinicalDto ToDto(TissGuide guide) => new(
        guide.Cid10Code,
        guide.Cid10Secondary,
        guide.ClinicalJustification,
        guide.ServiceCharacter,
        guide.AccidentIndicator,
        guide.RequestingProfessionalId,
        guide.RequestingProfessionalName,
        guide.RequestingProfessionalCrm,
        guide.ExecutingProfessionalId,
        guide.ExecutingProfessionalName,
        guide.ExecutingProfessionalCrm,
        guide.AdmissionDate,
        guide.DischargeDate,
        guide.RequestedBedType,
        guide.ParentGuideId,
        guide.ProfessionalRole,
        guide.ParticipationPercent,
        guide.SurgeryId);

    public static void Apply(TissGuide guide, TissGuideClinicalRequest? clinical)
    {
        if (clinical is null)
            return;

        guide.Cid10Code = clinical.Cid10Code?.Trim();
        guide.Cid10Secondary = clinical.Cid10Secondary?.Trim();
        guide.ClinicalJustification = clinical.ClinicalJustification?.Trim();
        guide.ServiceCharacter = clinical.ServiceCharacter;
        guide.AccidentIndicator = clinical.AccidentIndicator;
        guide.RequestingProfessionalId = clinical.RequestingProfessionalId;
        guide.RequestingProfessionalName = clinical.RequestingProfessionalName?.Trim();
        guide.RequestingProfessionalCrm = clinical.RequestingProfessionalCrm?.Trim();
        guide.ExecutingProfessionalId = clinical.ExecutingProfessionalId;
        guide.ExecutingProfessionalName = clinical.ExecutingProfessionalName?.Trim();
        guide.ExecutingProfessionalCrm = clinical.ExecutingProfessionalCrm?.Trim();
        guide.AdmissionDate = clinical.AdmissionDate;
        guide.DischargeDate = clinical.DischargeDate;
        guide.RequestedBedType = clinical.RequestedBedType?.Trim();
        guide.ParentGuideId = clinical.ParentGuideId;
        guide.ProfessionalRole = clinical.ProfessionalRole;
        guide.ParticipationPercent = clinical.ParticipationPercent;
        guide.SurgeryId = clinical.SurgeryId;
    }
}
