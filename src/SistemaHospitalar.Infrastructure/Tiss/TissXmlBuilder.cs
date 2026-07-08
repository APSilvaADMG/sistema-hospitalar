using System.Text;
using System.Xml.Linq;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Tiss;

public static class TissXmlBuilder
{
    private static readonly XNamespace Ans = TissXmlHash.AnsNamespace;

    public static string Build(
        HealthInsurance insurer,
        string batchNumber,
        string competence,
        IReadOnlyList<TissGuide> guides)
    {
        var version = string.IsNullOrWhiteSpace(insurer.TissVersion) ? "4.03.00" : insurer.TissVersion.Trim();
        var competenceAns = competence.Replace("-", "", StringComparison.Ordinal);
        var now = DateTime.Now;
        var sequential = now.ToString("yyyyMMddHHmmss");

        var guiasTiss = new XElement(Ans + "guiasTISS");
        foreach (var guide in guides)
            guiasTiss.Add(TissGuideXmlFactory.BuildTypedGuide(guide));

        var mensagem = new XElement(
            Ans + "mensagemTISS",
            new XAttribute(XNamespace.Xmlns + "ans", Ans.NamespaceName),
            new XElement(
                Ans + "cabecalho",
                new XElement(
                    Ans + "identificacaoTransacao",
                    new XElement(Ans + "tipoTransacao", "ENVIO_LOTE_GUIAS"),
                    new XElement(Ans + "sequencialTransacao", sequential),
                    new XElement(Ans + "dataRegistroTransacao", now.ToString("yyyy-MM-dd")),
                    new XElement(Ans + "horaRegistroTransacao", now.ToString("HH:mm:ss"))),
                new XElement(Ans + "Padrao", version),
                new XElement(Ans + "registroANS", insurer.AnsRegistration ?? "000000")),
            new XElement(
                Ans + "prestadorParaOperadora",
                new XElement(
                    Ans + "loteGuias",
                    new XElement(Ans + "numeroLote", batchNumber),
                    new XElement(Ans + "competencia", competenceAns),
                    new XElement(Ans + "quantidadeGuias", guides.Count),
                    guiasTiss)));

        var doc = new XDocument(new XDeclaration("1.0", "ISO-8859-1", null), mensagem);
        var hash = TissXmlHash.Compute(doc);
        mensagem.Add(new XElement(Ans + "epilogo", new XElement(Ans + "hash", hash)));

        return Serialize(doc);
    }

    private static string Serialize(XDocument doc)
    {
        var sb = new StringBuilder();
        if (doc.Declaration is not null)
            sb.AppendLine($"<?xml version=\"{doc.Declaration.Version}\" encoding=\"{doc.Declaration.Encoding}\"?>");

        sb.Append(doc.ToString(SaveOptions.DisableFormatting));
        return sb.ToString();
    }
}
