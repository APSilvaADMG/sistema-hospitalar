using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Tiss;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class HealthInsuranceCatalogSeed
{
    public static async Task EnsureAsync(
        AppDbContext dbContext,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var catalog = GetCatalogItems();
        var existing = await dbContext.HealthInsurances.ToListAsync(cancellationToken);
        var added = 0;
        var updated = 0;

        foreach (var item in catalog)
        {
            var match = FindMatch(existing, item);
            if (match is null)
            {
                dbContext.HealthInsurances.Add(item.ToEntity());
                added++;
                continue;
            }

            if (ApplyUpdates(match, item))
            {
                updated++;
            }
        }

        if (added == 0 && updated == 0)
        {
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger?.LogInformation(
            "Catálogo de convênios: {Added} adicionados, {Updated} atualizados.",
            added,
            updated);
    }

    public static IReadOnlyList<HealthInsuranceCatalogItem> GetCatalogItems() =>
    [
        Item("Particular", null, null, null, logoPath: "/insurers/particular.svg"),
        Item("SUS", null, null, "gov.br", logoPath: "/insurers/sus.svg"),
        Tiss("Alice", "421928", "34266553000102", "alice.com.br"),
        Tiss("Allianz Saúde", "000515", "04439627000102", "allianz.com.br", "ALLIANZ"),
        Tiss("Ameplan Saúde", "394734", "67839969000121", "ameplansaude.com.br"),
        Tiss("Amil", "326305", "29309127000179", "amil.com.br", "AMIL"),
        Tiss("Ampla Saúde", "422720", "41077489000187", "amplasaude.com.br"),
        Tiss("Ana Costa Saúde", "360244", "02864364000145", "anacostasaude.com.br"),
        Tiss("Ativia Saúde", "320510", "69289171000189", "ativia.com.br"),
        Tiss("Biosaúde", "402966", "03123146000112", "biosaude.com.br"),
        Tiss("Biovida Saúde", "415111", "04299138000194", "biovida.com.br"),
        Tiss("Blue", "423173", "44477823000188", "bluesaude.com.br"),
        Tiss("Blue Med Saúde", "344800", "62511019000150", "bluemed.com.br"),
        Tiss("Bradesco Saúde", "005711", "92693118000160", "bradescoseguros.com.br", "BRADESCO"),
        Tiss("Caixa Saúde", "418072", "13223975000120", "caixaseguradora.com.br"),
        Tiss("Care Plus", "379956", "02725347000127", "careplus.com.br"),
        Tiss("Classes Laboriosas", "394734", "67839969000121", "ameplansaude.com.br"),
        Tiss("Cruz Azul Saúde", "411752", "03849449000117", "cruzazulsaude.com.br"),
        Tiss("Cuidar.me", "422606", "38240036000115", "cuidar.me"),
        Tiss("Garantia de Saúde", "343064", "45572583000163", "garantiasaude.com.br"),
        Tiss("Go Care Saúde", "422681", "40187311000126", "gocare.com.br"),
        Tiss("Golden Cross", "403911", "01518211000183", "goldencross.com.br", "GOLDENCROSS"),
        Tiss("Greenline Saúde", "325074", "61849980000196", "greenline.com.br"),
        Tiss("Hapvida", "368253", "63554067000198", "hapvida.com.br", "HAPVIDA"),
        Tiss("HBC Saúde", "414352", "05011316000100", "hbcsaude.com.br"),
        Tiss("Health Santaris", "413194", "04004287000189", "santaris.com.br"),
        Tiss("Interclínicas", "420841", "22694698000125", "interclinicas.com.br"),
        Tiss("Medical Health", "400190", "02282844000106", "medicalhealth.com.br"),
        Tiss("MedSenior Saúde", "335614", "31466949000105", "medsenior.com.br"),
        Tiss("Med Tour Saúde", "328537", "00453863000114", "medtour.com.br"),
        Tiss("Notre Dame", "359017", "44649812000138", "notredameintermedica.com.br", "GNDI"),
        Tiss("One Health", "326305", "29309127000179", "onehealth.com.br", "ONEHEALTH"),
        Tiss("Plansaúde", "419362", "03897847000109", "plansaude.com.br"),
        Tiss("Plena Saúde", "348830", "00338763000147", "plenasaude.com.br"),
        Tiss("Porto Seguro", "000582", "04540010000170", "portoseguro.com.br", "PORTO"),
        Tiss("Prevent Senior", "302147", "00461479000163", "preventsenior.com.br"),
        Tiss("QSaúde", "421669", "30821576000180", "qsaude.com.br"),
        Tiss("Sagrada Família", "422371", "02753398000162", "sagradafamilia.org.br"),
        Tiss("Sami", "422398", "36567721000125", "sami.com.br"),
        Tiss("Santa Casa Mauá", "421197", "08225953000160", "santacasamaua.org.br"),
        Tiss("Santa Helena", "355097", "43293604000186", "santahelena.com.br"),
        Tiss("Santa Saúde", "418021", "13001218000102", "santasaude.com.br"),
        Tiss("São Camilo Saúde", "318299", "83506030000100", "saocamilo.com.br"),
        Tiss("São Cristóvão", "314218", "60975174000100", "saocristovao.com.br"),
        Tiss("São Miguel Saúde", "325236", "66854779000110", "saomiguelsaude.com.br"),
        Tiss("SB Saúde", "421154", "28633372000174", "sbsaude.com.br"),
        Tiss("Select Saúde", "358053", "37035441000139", "selectsaude.com.br"),
        Tiss("Seguros Unimed", "000701", "04487255000181", "segurosunimed.com.br", "UNIMED"),
        Tiss("Sompo Saúde", "000477", "47184510000120", "sompo.com.br"),
        Tiss("Sul América Saúde", "006246", "01685053000156", "sulamerica.com.br", "SULAMERICA"),
        Tiss("Total Medcare", "318477", "02888465000156", "totalmedcare.com.br"),
        Tiss("Trasmontano", "303623", "62638374000194", "trasmontano.com.br"),
        Tiss("Unica Saúde", "421944", "33379144000150", "unica.com.br"),
        Tiss("Unihosp Saúde", "385255", "01445199000124", "unihosp.com.br"),
        Tiss("Unimed Guarulhos", "333051", "74466137000172", "unimed.coop.br", "UNIMED-GUA"),
        Tiss("Unimed Jundiaí", "303267", "56727134000163", "unimed.coop.br", "UNIMED-JUN"),
        Tiss("Unimed Santos", "355721", "02812468000106", "unimed.coop.br", "UNIMED-STO"),
        Tiss("Unimed Nacional", "339679", "58229691000422", "unimednacional.coop.br", "UNIMED-NAC"),
    ];

    private static HealthInsurance? FindMatch(
        IReadOnlyList<HealthInsurance> existing,
        HealthInsuranceCatalogItem item)
    {
        var normalized = NormalizeName(item.Name);
        var byName = existing.FirstOrDefault(e => NormalizeName(e.Name) == normalized);
        if (byName is not null)
        {
            return byName;
        }

        var byAlias = existing.FirstOrDefault(e => IsAlias(e.Name, item.Name));
        if (byAlias is not null)
        {
            return byAlias;
        }

        if (!string.IsNullOrWhiteSpace(item.Cnpj))
        {
            var cnpjMatches = existing
                .Where(e => !string.IsNullOrWhiteSpace(e.Cnpj) && e.Cnpj == item.Cnpj)
                .ToList();
            if (cnpjMatches.Count == 1)
            {
                return cnpjMatches[0];
            }
        }

        if (!string.IsNullOrWhiteSpace(item.AnsRegistration))
        {
            var ans = NormalizeAns(item.AnsRegistration);
            var ansMatches = existing
                .Where(e =>
                    !string.IsNullOrWhiteSpace(e.AnsRegistration) &&
                    NormalizeAns(e.AnsRegistration) == ans)
                .ToList();
            if (ansMatches.Count == 1)
            {
                return ansMatches[0];
            }
        }

        return null;
    }

    private static bool IsAlias(string existingName, string catalogName)
    {
        var a = NormalizeName(existingName);
        var b = NormalizeName(catalogName);

        return (a, b) switch
        {
            ("unimed", "seguros unimed") => true,
            ("sulamerica", "sul america saude") => true,
            ("porto saude", "porto seguro") => true,
            _ => false,
        };
    }

    private static bool ApplyUpdates(HealthInsurance entity, HealthInsuranceCatalogItem item)
    {
        var changed = false;

        if (entity.Name != item.Name && IsAlias(entity.Name, item.Name))
        {
            entity.Name = item.Name;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(entity.AnsRegistration) && item.AnsRegistration is not null)
        {
            entity.AnsRegistration = item.AnsRegistration;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(entity.Cnpj) && item.Cnpj is not null)
        {
            entity.Cnpj = item.Cnpj;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(entity.LogoUrl) && item.LogoUrl is not null)
        {
            entity.LogoUrl = item.LogoUrl;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(entity.WebsiteUrl) && item.WebsiteUrl is not null)
        {
            entity.WebsiteUrl = item.WebsiteUrl;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(entity.OperatorCode) && item.OperatorCode is not null)
        {
            entity.OperatorCode = item.OperatorCode;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(entity.TissVersion) && item.TissVersion is not null)
        {
            entity.TissVersion = item.TissVersion;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(entity.PortalUrl) && item.PortalUrl is not null)
        {
            entity.PortalUrl = item.PortalUrl;
            changed = true;
        }

        var profile = OperatorIntegrationProfiles.FindByName(entity.Name)
            ?? (item.OperatorCode is not null ? OperatorIntegrationProfiles.FindByOperatorCode(item.OperatorCode) : null);
        if (profile is not null)
        {
            var before = $"{entity.AuthorizationDeadlineDays}|{entity.RequiresOnlineAuthorization}|{entity.BusinessRules}|{entity.WebServiceUrl}";
            OperatorIntegrationProfiles.ApplyTo(profile, entity);
            var after = $"{entity.AuthorizationDeadlineDays}|{entity.RequiresOnlineAuthorization}|{entity.BusinessRules}|{entity.WebServiceUrl}";
            if (before != after)
                changed = true;
        }

        return changed;
    }

    private static HealthInsuranceCatalogItem Tiss(
        string name,
        string ans,
        string cnpj,
        string domain,
        string? operatorCode = null) =>
        Item(name, ans, cnpj, domain, operatorCode, tiss: true);

    private static HealthInsuranceCatalogItem Item(
        string name,
        string? ans,
        string? cnpj,
        string? domain,
        string? operatorCode = null,
        bool tiss = false,
        string? logoPath = null)
    {
        var website = domain is null ? null : $"https://www.{domain}";
        var logo = logoPath ?? (domain is null ? null : FaviconUrl(domain));
        var portal = tiss && domain is not null ? website : null;

        return new HealthInsuranceCatalogItem(
            name,
            ans,
            cnpj,
            logo,
            website,
            tiss ? "4.03.00" : null,
            operatorCode,
            portal);
    }

    private static string FaviconUrl(string domain) =>
        $"https://www.google.com/s2/favicons?domain={domain}&sz=128";

    private static string NormalizeAns(string? value) =>
        new string((value ?? string.Empty).Where(char.IsDigit).ToArray()).TrimStart('0');

    private static string NormalizeName(string value) =>
        new string(value.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray())
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace(".", string.Empty)
            .Replace("-", string.Empty);

    public sealed record HealthInsuranceCatalogItem(
        string Name,
        string? AnsRegistration,
        string? Cnpj,
        string? LogoUrl,
        string? WebsiteUrl,
        string? TissVersion,
        string? OperatorCode,
        string? PortalUrl)
    {
        public HealthInsurance ToEntity()
        {
            var entity = new HealthInsurance
            {
                Name = Name,
                AnsRegistration = AnsRegistration,
                Cnpj = Cnpj,
                LogoUrl = LogoUrl,
                WebsiteUrl = WebsiteUrl,
                TissVersion = TissVersion,
                OperatorCode = OperatorCode,
                PortalUrl = PortalUrl,
                UseMockIntegration = true,
            };

            var profile = OperatorIntegrationProfiles.FindByName(Name)
                ?? (OperatorCode is not null ? OperatorIntegrationProfiles.FindByOperatorCode(OperatorCode) : null);
            if (profile is not null)
                OperatorIntegrationProfiles.ApplyTo(profile, entity);

            return entity;
        }
    }
}
