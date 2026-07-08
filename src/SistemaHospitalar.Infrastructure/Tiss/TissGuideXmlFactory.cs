using System.Globalization;
using System.Xml.Linq;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Tiss;

internal static class TissGuideXmlFactory
{
    private static readonly XNamespace Ans = TissXmlHash.AnsNamespace;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static XElement BuildTypedGuide(TissGuide guide)
    {
        return guide.GuideType switch
        {
            TissGuideType.Consultation => Wrap("guiaConsulta", BuildConsultation(guide)),
            TissGuideType.SpSadt => Wrap("guiaSP_SADT", BuildSpSadt(guide)),
            TissGuideType.Hospitalization or TissGuideType.DischargeSummary
                => Wrap("guiaResumoInternacao", BuildHospitalization(guide)),
            TissGuideType.IndividualFees => Wrap("guiaHonorarioIndividual", BuildHonorarios(guide)),
            TissGuideType.DentalTreatment => Wrap("guiaOdontologia", BuildDental(guide)),
            _ => BuildLegacyGuia(guide),
        };
    }

    private static XElement Wrap(string elementName, IEnumerable<XElement> children)
        => new(Ans + elementName, children);

    private static IEnumerable<XElement> BuildConsultation(TissGuide guide)
    {
        yield return BuildIdentificacaoGuia(guide);
        yield return BuildBeneficiario(guide);
        yield return BuildContratado();
        yield return BuildProfissionalExecutante(guide);
        if (!string.IsNullOrWhiteSpace(guide.Cid10Code))
        {
            yield return new XElement(
                Ans + "hipoteseDiagnostica",
                new XElement(Ans + "diagnosticoPrincipal", guide.Cid10Code));
        }

        yield return new XElement(
            Ans + "dadosAtendimento",
            BuildProcedimentoElements(guide));
    }

    private static IEnumerable<XElement> BuildSpSadt(TissGuide guide)
    {
        yield return BuildIdentificacaoGuia(guide);
        yield return BuildBeneficiario(guide);
        yield return BuildContratado();
        yield return BuildProfissionalExecutante(guide);
        if (!string.IsNullOrWhiteSpace(guide.AuthorizationPassword))
            yield return new XElement(Ans + "senhaAutorizacao", guide.AuthorizationPassword);
        yield return new XElement(
            Ans + "procedimentosExecutados",
            BuildProcedimentoElements(guide));
        yield return new XElement(Ans + "valorTotal", FormatMoney(guide.TotalAmount));
    }

    private static IEnumerable<XElement> BuildHospitalization(TissGuide guide)
    {
        yield return BuildIdentificacaoGuia(guide);
        yield return BuildBeneficiario(guide);
        yield return BuildContratado();
        if (guide.AdmissionDate.HasValue)
            yield return new XElement(Ans + "dataAdmissao", guide.AdmissionDate.Value.ToString("yyyy-MM-dd"));
        if (guide.DischargeDate.HasValue)
            yield return new XElement(Ans + "dataAlta", guide.DischargeDate.Value.ToString("yyyy-MM-dd"));
        if (!string.IsNullOrWhiteSpace(guide.Cid10Code))
            yield return new XElement(Ans + "diagnosticoPrincipal", guide.Cid10Code);
        yield return new XElement(
            Ans + "procedimentosExecutados",
            BuildProcedimentoElements(guide));
        yield return new XElement(Ans + "valorTotal", FormatMoney(guide.TotalAmount));
    }

    private static IEnumerable<XElement> BuildHonorarios(TissGuide guide)
    {
        yield return BuildIdentificacaoGuia(guide);
        yield return BuildBeneficiario(guide);
        yield return BuildContratado();
        yield return BuildProfissionalExecutante(guide);
        yield return new XElement(
            Ans + "procedimentosExamesRealizados",
            BuildProcedimentoElements(guide));
        yield return new XElement(Ans + "valorTotal", FormatMoney(guide.TotalAmount));
    }

    private static IEnumerable<XElement> BuildDental(TissGuide guide)
    {
        yield return new XElement(
            Ans + "cabecalhoGuia",
            new XElement(Ans + "numeroGuiaPrestador", guide.GuideNumber));
        yield return BuildBeneficiario(guide);
        yield return new XElement(
            Ans + "procedimentosExecutados",
            BuildProcedimentoElements(guide));
        yield return new XElement(Ans + "valorTotal", FormatMoney(guide.TotalAmount));
    }

    private static XElement BuildLegacyGuia(TissGuide guide)
    {
        var guia = new XElement(
            Ans + "guia",
            new XElement(Ans + "numeroGuiaPrestador", guide.GuideNumber),
            new XElement(Ans + "tipoGuia", (int)guide.GuideType),
            new XElement(Ans + "carteiraBeneficiario", guide.BeneficiaryCardNumber ?? string.Empty),
            new XElement(Ans + "nomeBeneficiario", guide.Patient?.FullName ?? string.Empty),
            new XElement(Ans + "plano", guide.BeneficiaryPlanName ?? string.Empty));

        AppendCommonFields(guia, guide);
        guia.Add(new XElement(Ans + "valorTotal", FormatMoney(guide.TotalAmount)));
        foreach (var proc in BuildProcedimentoElements(guide))
            guia.Add(proc);
        return guia;
    }

    private static XElement BuildIdentificacaoGuia(TissGuide guide)
        => new(
            Ans + "identificacaoGuia",
            new XElement(Ans + "numeroGuiaPrestador", guide.GuideNumber));

    private static XElement BuildBeneficiario(TissGuide guide)
        => new(
            Ans + "beneficiario",
            new XElement(Ans + "numeroCarteira", guide.BeneficiaryCardNumber ?? string.Empty),
            new XElement(Ans + "nomeBeneficiario", guide.Patient?.FullName ?? string.Empty),
            new XElement(Ans + "nomePlano", guide.BeneficiaryPlanName ?? string.Empty),
            string.IsNullOrWhiteSpace(guide.BeneficiaryCns)
                ? null
                : new XElement(Ans + "numeroCNS", guide.BeneficiaryCns));

    private static XElement BuildContratado()
        => new(
            Ans + "dadosContratado",
            new XElement(Ans + "codigoPrestadorNaOperadora", "000000000000000"));

    private static XElement BuildProfissionalExecutante(TissGuide guide)
    {
        var name = guide.ExecutingProfessionalName ?? guide.RequestingProfessionalName ?? "PROFISSIONAL";
        var crm = guide.ExecutingProfessionalCrm ?? guide.RequestingProfessionalCrm ?? string.Empty;
        return new XElement(
            Ans + "profissionalExecutante",
            new XElement(Ans + "nomeProfissional", name),
            string.IsNullOrWhiteSpace(crm) ? null : new XElement(Ans + "numeroConselhoProfissional", crm),
            new XElement(Ans + "conselhoProfissional", "06"),
            new XElement(Ans + "UF", "35"));
    }

    private static IEnumerable<XElement> BuildProcedimentoElements(TissGuide guide)
    {
        foreach (var item in guide.Items.Where(i => i.IsActive))
        {
            yield return new XElement(
                Ans + "procedimentoRealizado",
                new XElement(Ans + "codigoProcedimento", item.TussCode),
                new XElement(Ans + "descricaoProcedimento", item.Description),
                new XElement(Ans + "quantidadeExecutada", item.Quantity),
                new XElement(Ans + "valorUnitario", FormatMoney(item.UnitPrice)),
                new XElement(Ans + "valorTotal", FormatMoney(item.UnitPrice * item.Quantity)));
        }
    }

    private static void AppendCommonFields(XElement guia, TissGuide guide)
    {
        if (!string.IsNullOrWhiteSpace(guide.Cid10Code))
            guia.Add(new XElement(Ans + "cidPrincipal", guide.Cid10Code));
        if (!string.IsNullOrWhiteSpace(guide.AuthorizationPassword))
            guia.Add(new XElement(Ans + "senhaAutorizacao", guide.AuthorizationPassword));
        if (!string.IsNullOrWhiteSpace(guide.RequestingProfessionalCrm))
            guia.Add(new XElement(Ans + "crmSolicitante", guide.RequestingProfessionalCrm));
        if (!string.IsNullOrWhiteSpace(guide.ExecutingProfessionalCrm))
            guia.Add(new XElement(Ans + "crmExecutante", guide.ExecutingProfessionalCrm));
        if (guide.AdmissionDate.HasValue)
            guia.Add(new XElement(Ans + "dataAdmissao", guide.AdmissionDate.Value.ToString("yyyy-MM-dd")));
        if (guide.DischargeDate.HasValue)
            guia.Add(new XElement(Ans + "dataAlta", guide.DischargeDate.Value.ToString("yyyy-MM-dd")));
    }

    private static string FormatMoney(decimal value) => value.ToString("F2", Inv);
}
