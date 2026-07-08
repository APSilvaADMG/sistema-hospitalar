using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using SistemaHospitalar.Application.DTOs.Tiss;

namespace SistemaHospitalar.Infrastructure.Tiss;

public static class TissXmlValidator
{
    private static readonly string SchemaRoot = Path.Combine(AppContext.BaseDirectory, "Tiss", "Schemas");

    public static TissXmlValidationResultDto Validate(string? xmlContent)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            return new TissXmlValidationResultDto(
                false, null, false, null, null, null,
                "XML vazio.",
                ["Conteúdo XML não informado."]);
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(xmlContent, LoadOptions.PreserveWhitespace);
        }
        catch (Exception ex)
        {
            return new TissXmlValidationResultDto(
                false, null, false, null, null, null,
                "XML malformado.",
                [$"Erro de parse: {ex.Message}"]);
        }

        var version = DetectVersion(document);
        var computedHash = TissXmlHash.Compute(document);
        var hashValid = TissXmlHash.TryValidateProvidedHash(document, out var providedHash, out _);
        if (!hashValid)
            errors.Add(providedHash is null
                ? "Epílogo/hash ausente no XML."
                : $"Hash inválido. Fornecido: {providedHash}, calculado: {computedHash}.");

        bool? schemaValid = null;
        string? schemaMessage = null;
        var schemaPath = ResolveSchemaPath(version);
        if (schemaPath is null)
        {
            schemaMessage = version is null
                ? "Versão TISS não detectada; validação XSD ignorada."
                : $"Schema XSD não disponível localmente para a versão {version}. Hash e estrutura básica foram verificados.";
        }
        else
        {
            var schemaErrors = ValidateAgainstXsd(document, schemaPath);
            if (schemaErrors.Count == 0)
            {
                schemaValid = true;
                schemaMessage = $"XML compatível com o schema {Path.GetFileName(schemaPath)}.";
            }
            else
            {
                schemaValid = false;
                errors.AddRange(schemaErrors);
                schemaMessage = $"Falha na validação XSD ({Path.GetFileName(schemaPath)}).";
            }
        }

        var isValid = hashValid && (schemaValid is null or true);
        return new TissXmlValidationResultDto(
            isValid,
            version,
            hashValid,
            computedHash,
            providedHash,
            schemaValid,
            schemaMessage,
            errors);
    }

    private static string? DetectVersion(XDocument document)
    {
        var ans = (XNamespace)TissXmlHash.AnsNamespace;
        var root = document.Root;
        if (root is null)
            return null;

        var padrao = root.Descendants(ans + "Padrao").FirstOrDefault()?.Value
            ?? root.Descendants(ans + "versaoPadrao").FirstOrDefault()?.Value;

        return string.IsNullOrWhiteSpace(padrao) ? null : padrao.Trim();
    }

    private static string? ResolveSchemaPath(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return null;

        var fileVersion = version.Trim().Replace('.', '_');
        var candidates = new[]
        {
            Path.Combine(SchemaRoot, $"tissV{fileVersion}.xsd"),
            Path.Combine(SchemaRoot, $"tissMonitoramentoV{fileVersion}.xsd"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static List<string> ValidateAgainstXsd(XDocument document, string schemaPath)
    {
        var errors = new List<string>();
        var schemas = new XmlSchemaSet();
        schemas.Add(null, schemaPath);

        try
        {
            document.Validate(schemas, (_, e) =>
            {
                errors.Add($"Linha {e.Exception?.LineNumber}, coluna {e.Exception?.LinePosition}: {e.Message}");
            });
        }
        catch (Exception ex)
        {
            errors.Add($"Erro ao validar XSD: {ex.Message}");
        }

        return errors;
    }
}
