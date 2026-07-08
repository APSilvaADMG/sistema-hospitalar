using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.BusinessRules;

public static class PatientRegistrationRules
{
    public static string ResolveRegistrationCpf(bool usesResponsibleCpf, string? patientCpf, LegalResponsibleData? legalResponsible)
    {
        if (usesResponsibleCpf)
        {
            ValidateLegalResponsible(legalResponsible);
            return PatientCpfRules.Normalize(legalResponsible!.Cpf);
        }

        var normalized = PatientCpfRules.Normalize(patientCpf);
        if (PatientCpfRules.IsMissing(normalized))
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.PatientCpfRequired}] CPF é obrigatório para cadastro de paciente.");
        }

        PatientCpfRules.ValidateFormat(normalized);
        return normalized;
    }

    public static void ValidateLegalResponsible(LegalResponsibleData? legalResponsible)
    {
        if (legalResponsible is null)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.LegalResponsibleRequired}] Informe os dados do responsável legal.");
        }

        if (string.IsNullOrWhiteSpace(legalResponsible.Name))
        {
            throw new InvalidOperationException("Informe o nome do responsável legal.");
        }

        var cpf = PatientCpfRules.Normalize(legalResponsible.Cpf);
        PatientCpfRules.ValidateFormat(cpf);

        if (HospitalBusinessRules.CalculateAgeYears(legalResponsible.BirthDate) < HospitalBusinessRules.MinorAgeYears)
        {
            throw new InvalidOperationException(
                $"[{BusinessRuleCodes.LegalResponsibleAdult}] O responsável legal deve ser maior de 18 anos.");
        }

        if (string.IsNullOrWhiteSpace(legalResponsible.Rg))
        {
            throw new InvalidOperationException(
                "Informe o documento de identificação (RG) do responsável legal.");
        }

        if (legalResponsible.Relationship == LegalResponsibleRelationship.NotInformed)
        {
            throw new InvalidOperationException(
                "Informe o parentesco do responsável legal (pai, mãe, cônjuge ou outro).");
        }

        if (legalResponsible.Relationship == LegalResponsibleRelationship.Other)
        {
            if (legalResponsible.AuthorizationDocumentType is null
                or LegalAuthorizationDocumentType.NotInformed)
            {
                throw new InvalidOperationException(
                    $"[{BusinessRuleCodes.LegalAuthorizationRequired}] Para parentesco \"Outro\", informe o documento formal de autorização " +
                    "(Termo de Curatela, Termo de Guarda ou Procuração Pública).");
            }

            if (string.IsNullOrWhiteSpace(legalResponsible.AuthorizationDocumentReference))
            {
                throw new InvalidOperationException(
                    "Informe a referência/número do documento formal de autorização.");
            }
        }
        else if (legalResponsible.Relationship is not (
            LegalResponsibleRelationship.Father
            or LegalResponsibleRelationship.Mother
            or LegalResponsibleRelationship.Spouse))
        {
            throw new InvalidOperationException(
                "Parentesco permitido: pai, mãe, cônjuge ou outro (com documentação formal).");
        }
    }
}

public record LegalResponsibleData(
    string Name,
    string Cpf,
    DateOnly BirthDate,
    LegalResponsibleRelationship Relationship,
    string Rg,
    LegalAuthorizationDocumentType? AuthorizationDocumentType,
    string? AuthorizationDocumentReference);
