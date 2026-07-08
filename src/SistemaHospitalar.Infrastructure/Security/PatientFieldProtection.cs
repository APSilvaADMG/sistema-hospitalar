using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.BusinessRules;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Security;

public static class PatientFieldProtection
{
    public static void Protect(Patient patient, IFieldEncryptionService encryption, string normalizedCpf)
    {
        patient.CpfHash = encryption.HashForLookup(normalizedCpf);
        patient.Cpf = encryption.Encrypt(normalizedCpf);
        ProtectSensitiveFields(patient, encryption);
    }

    public static void ProtectWithoutCpf(Patient patient, IFieldEncryptionService encryption)
    {
        patient.CpfHash = null;
        patient.Cpf = string.Empty;
        ProtectSensitiveFields(patient, encryption);
    }

    private static void ProtectSensitiveFields(Patient patient, IFieldEncryptionService encryption)
    {
        if (!string.IsNullOrWhiteSpace(patient.Cns))
        {
            var normalizedCns = PatientCnsRules.Normalize(patient.Cns);
            if (normalizedCns.Length == PatientCnsRules.RequiredLength)
            {
                patient.CnsHash = encryption.HashForLookup(normalizedCns);
            }
        }
        else
        {
            patient.CnsHash = null;
        }

        patient.Cns = EncryptOptional(encryption, patient.Cns);
        patient.Rg = EncryptOptional(encryption, patient.Rg);
        patient.LegalResponsibleRg = EncryptOptional(encryption, patient.LegalResponsibleRg);
        patient.Email = EncryptOptional(encryption, patient.Email);
        patient.Phone = EncryptOptional(encryption, patient.Phone);
        patient.MobilePhone = EncryptOptional(encryption, patient.MobilePhone);
        patient.EmergencyContactPhone = EncryptOptional(encryption, patient.EmergencyContactPhone);
        patient.AddressStreet = EncryptOptional(encryption, patient.AddressStreet);
        patient.AddressNumber = EncryptOptional(encryption, patient.AddressNumber);
        patient.AddressComplement = EncryptOptional(encryption, patient.AddressComplement);
        patient.AddressNeighborhood = EncryptOptional(encryption, patient.AddressNeighborhood);
        patient.AddressZipCode = EncryptOptional(encryption, patient.AddressZipCode);
    }

    public static void ApplyCnsProtection(Patient patient, IFieldEncryptionService encryption)
    {
        if (!string.IsNullOrWhiteSpace(patient.Cns))
        {
            var normalizedCns = PatientCnsRules.Normalize(patient.Cns);
            if (normalizedCns.Length == PatientCnsRules.RequiredLength)
            {
                patient.CnsHash = encryption.HashForLookup(normalizedCns);
            }
        }
        else
        {
            patient.CnsHash = null;
        }

        patient.Cns = EncryptOptional(encryption, patient.Cns);
    }

    public static void ReprotectCpf(Patient patient, IFieldEncryptionService encryption)
    {
        if (patient.UsesResponsibleCpf)
        {
            if (patient.CpfHash is null)
            {
                throw new InvalidOperationException("Prontuário com CPF do responsável sem hash de referência.");
            }

            var normalizedCpf = PatientCpfRules.Normalize(patient.Cpf);
            Protect(patient, encryption, normalizedCpf);
            return;
        }

        if (patient.CpfHash is null)
        {
            ProtectWithoutCpf(patient, encryption);
            return;
        }

        var cpf = PatientCpfRules.Normalize(patient.Cpf);
        Protect(patient, encryption, cpf);
    }

    public static void Decrypt(Patient patient, IFieldEncryptionService encryption)
    {
        patient.Cpf = DecryptField(encryption, patient.Cpf);
        patient.Cns = DecryptOptional(encryption, patient.Cns);
        patient.Rg = DecryptOptional(encryption, patient.Rg);
        patient.LegalResponsibleRg = DecryptOptional(encryption, patient.LegalResponsibleRg);
        patient.Email = DecryptOptional(encryption, patient.Email);
        patient.Phone = DecryptOptional(encryption, patient.Phone);
        patient.MobilePhone = DecryptOptional(encryption, patient.MobilePhone);
        patient.EmergencyContactPhone = DecryptOptional(encryption, patient.EmergencyContactPhone);
        patient.AddressStreet = DecryptOptional(encryption, patient.AddressStreet);
        patient.AddressNumber = DecryptOptional(encryption, patient.AddressNumber);
        patient.AddressComplement = DecryptOptional(encryption, patient.AddressComplement);
        patient.AddressNeighborhood = DecryptOptional(encryption, patient.AddressNeighborhood);
        patient.AddressZipCode = DecryptOptional(encryption, patient.AddressZipCode);
    }

    private static string DecryptField(IFieldEncryptionService encryption, string value)
        => encryption.IsEncrypted(value) ? encryption.Decrypt(value) : value;

    private static string? DecryptOptional(IFieldEncryptionService encryption, string? value)
        => string.IsNullOrEmpty(value) ? value : DecryptField(encryption, value);

    private static string? EncryptOptional(IFieldEncryptionService encryption, string? value)
        => string.IsNullOrEmpty(value) ? value : encryption.Encrypt(value);
}
