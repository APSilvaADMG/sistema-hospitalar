using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Common;

public static class WardInsuranceMatcher
{
    public static WardCoverageModality ResolvePatientModality(string? insuranceName)
    {
        if (string.IsNullOrWhiteSpace(insuranceName))
        {
            return WardCoverageModality.Particular;
        }

        if (insuranceName.Equals("SUS", StringComparison.OrdinalIgnoreCase))
        {
            return WardCoverageModality.Sus;
        }

        if (insuranceName.Equals("Particular", StringComparison.OrdinalIgnoreCase))
        {
            return WardCoverageModality.Particular;
        }

        return WardCoverageModality.Convenio;
    }

    public static bool IsCompatible(WardCoverageModality wardModality, WardCoverageModality patientModality) =>
        wardModality == WardCoverageModality.Mixed || wardModality == patientModality;

    public static string ModalityLabel(WardCoverageModality modality) => modality switch
    {
        WardCoverageModality.Particular => "Particular",
        WardCoverageModality.Convenio => "Convênio",
        WardCoverageModality.Sus => "SUS",
        WardCoverageModality.Mixed => "Mista",
        _ => modality.ToString()
    };
}
