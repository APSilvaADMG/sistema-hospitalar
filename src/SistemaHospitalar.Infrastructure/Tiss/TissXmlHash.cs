using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace SistemaHospitalar.Infrastructure.Tiss;

/// <summary>
/// Cálculo do hash MD5 do epílogo TISS (concatenação dos valores-folha, ISO-8859-1).
/// Portado do algoritmo do projeto tiss-master (Python).
/// </summary>
public static class TissXmlHash
{
    public const string AnsNamespace = "http://www.ans.gov.br/padroes/tiss/schemas";

    public static string Compute(XDocument document)
    {
        var clone = new XDocument(document);
        var root = clone.Root ?? throw new InvalidOperationException("XML TISS sem elemento raiz.");
        var ans = (XNamespace)AnsNamespace;

        root.Element(ans + "epilogo")?.Remove();

        var leafValues = root
            .Descendants()
            .Where(e => !e.HasElements)
            .Select(e => e.Value)
            .Where(v => v is not null);

        var content = string.Concat(leafValues);
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(content);
        return Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant();
    }

    public static bool TryValidateProvidedHash(XDocument document, out string? provided, out string computed)
    {
        provided = null;
        computed = Compute(document);

        var ans = (XNamespace)AnsNamespace;
        provided = document.Root?
            .Element(ans + "epilogo")?
            .Element(ans + "hash")?
            .Value;

        if (string.IsNullOrWhiteSpace(provided))
            return false;

        return string.Equals(provided.Trim(), computed, StringComparison.OrdinalIgnoreCase);
    }
}
