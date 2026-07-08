using SistemaHospitalar.Application.DTOs.Reports;

namespace SistemaHospitalar.Infrastructure.Reports;

/// <summary>
/// Rótulos alinhados aos templates R4EPI/sitrep (.Rmd), HospitalRun, dev-queiroz (Groq/PDF).
/// Repos clonados em Diversos/external-repos/ — sync-external-repos.ps1
/// </summary>
public static class ReportFieldMappings
{
    public record FieldMapping(
        string SourceRepo,
        string SourceTemplate,
        string? PrintSubtitle,
        string? DocumentType,
        IReadOnlyDictionary<string, string> ColumnLabels,
        IReadOnlyDictionary<string, string>? KpiLabels = null);

    private record PrefixMapping(
        string SourceRepo,
        string SourceTemplate,
        string? DocumentType,
        IReadOnlyDictionary<string, string>? ColumnOverrides = null);

    /// <summary>Colunas padrão para relatórios hospitalares (pt-BR).</summary>
    private static readonly Dictionary<string, string> CommonColumnLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["patient"] = "Paciente",
        ["name"] = "Nome",
        ["cpf"] = "CPF",
        ["cns"] = "CNS",
        ["date"] = "Data",
        ["count"] = "Quantidade (n)",
        ["doctor"] = "Profissional",
        ["professional"] = "Profissional",
        ["specialty"] = "Especialidade",
        ["urgency"] = "Nível de gravidade",
        ["complaint"] = "Queixa principal",
        ["arrivedAt"] = "Chegada",
        ["admittedAt"] = "Internação",
        ["dischargedAt"] = "Alta",
        ["diagnosis"] = "Diagnóstico",
        ["cid"] = "CID-10",
        ["procedure"] = "Procedimento",
        ["product"] = "Produto",
        ["sku"] = "SKU",
        ["quantity"] = "Quantidade (n)",
        ["ward"] = "Ala",
        ["bed"] = "Leito",
        ["status"] = "Situação",
        ["indicator"] = "Indicador",
        ["value"] = "Valor",
        ["week"] = "Semana epidemiológica",
        ["cases"] = "Casos (n)",
        ["deaths"] = "Óbitos (n)",
        ["population"] = "População em risco",
        ["arPer10000"] = "TA (por 10.000)",
        ["insurance"] = "Convênio",
        ["amount"] = "Valor (R$)",
        ["label"] = "Descrição",
    };

    private static readonly Dictionary<string, PrefixMapping> PrefixMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ccih."] = new(
            "R4EPI/sitrep",
            "inst/rmarkdown/templates/",
            "Relatório de Situação — CCIH"),
        ["er."] = new(
            "dev-queiroz/sistema-hospitalar",
            "src/utils/pdfCompiler.ts",
            "Relatório operacional — Pronto Socorro",
            new Dictionary<string, string>
            {
                ["urgency"] = "Nível de gravidade (Manchester)",
                ["complaint"] = "Queixa principal",
                ["arrivedAt"] = "Data/hora de chegada",
            }),
        ["lab."] = new(
            "HospitalRun/hospitalrun-frontend",
            "src/labs/",
            "Relatório Laboratorial"),
        ["pharmacy."] = new(
            "HospitalRun/hospitalrun-frontend",
            "src/medications/",
            "Relatório de Farmácia"),
        ["img."] = new(
            "HospitalRun/hospitalrun-frontend",
            "src/imagings/",
            "Relatório de Diagnóstico por Imagem"),
        ["supply."] = new(
            "HospitalRun/hospitalrun-frontend",
            "src/inventory/",
            "Relatório de Almoxarifado"),
        ["hosp."] = new(
            "FabiolaCosta/DataBase-Hospital",
            "SQL schema — internacao/leito",
            "Relatório de Internação"),
        ["admin."] = new(
            "Bayanno SGHC",
            "reports/administrative",
            "Relatório Administrativo Hospitalar"),
        ["reception."] = new(
            "Bayanno SGHC",
            "reports/reception",
            "Relatório de Recepção"),
        ["pep."] = new(
            "dev-queiroz/sistema-hospitalar",
            "src/utils/pdfCompiler.ts",
            "Prontuário Médico — Informações Gerais"),
        ["nursing."] = new(
            "Bayanno SGHC",
            "reports/nursing",
            "Relatório de Enfermagem"),
        ["surgery."] = new(
            "Bayanno SGHC",
            "reports/surgery",
            "Relatório Cirúrgico"),
        ["fin."] = new(
            "APSMedCore BI",
            "reports/financial",
            "Relatório Gerencial — Financeiro"),
        ["bi."] = new(
            "APSMedCore BI",
            "reports/bi",
            "Relatório Gerencial — BI"),
        ["ins."] = new(
            "APSMedCore",
            "reports/tiss",
            "Relatório TISS / Convênios"),
        ["bill."] = new(
            "APSMedCore",
            "reports/billing",
            "Faturamento Hospitalar"),
        ["reg."] = new(
            "DATASUS/SIA-SUS",
            "Exportação regulatória",
            "Relatório Regulatório SUS"),
        ["quality."] = new(
            "HospitalRun/hospitalrun-frontend",
            "src/incidents/",
            "Relatório de Qualidade e Segurança"),
        ["audit."] = new(
            "APSMedCore",
            "reports/audit",
            "Relatório de Auditoria"),
        ["hr."] = new(
            "Bayanno SGHC",
            "reports/hr",
            "Recursos Humanos"),
    };

    private static readonly Dictionary<string, FieldMapping> ExactMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ccih.epidemic.curve"] = new(
            "R4EPI/sitrep",
            "inst/rmarkdown/templates/measles_outbreak/skeleton/skeleton.Rmd",
            "Casos por semana de início",
            "Relatório de Situação — Curva epidêmica",
            new Dictionary<string, string>
            {
                ["week"] = "Semana epidemiológica",
                ["cases"] = "Casos (n)",
                ["population"] = "População em risco",
                ["arPer10000"] = "TA (por 10.000)",
            }),

        ["ccih.mortality.surveillance"] = new(
            "R4EPI/sitrep",
            "inst/rmarkdown/templates/mortality/skeleton/skeleton.Rmd",
            "Vigilância de mortalidade",
            "Relatório de Situação — Mortalidade",
            new Dictionary<string, string> { ["week"] = "Semana epidemiológica", ["deaths"] = "Óbitos (n)" }),

        ["ccih.vaccination.coverage"] = new(
            "R4EPI/sitrep",
            "inst/rmarkdown/templates/vaccination_long/skeleton/skeleton.Rmd",
            "Cobertura vacinal",
            "Relatório de Situação — Vacinação",
            new Dictionary<string, string>
            {
                ["vaccine"] = "Imunobiológico",
                ["doses"] = "Doses aplicadas (n)",
            }),

        ["ccih.outbreak.indicators"] = new(
            "dev-queiroz/sistema-hospitalar",
            "src/modulo/ia/service/GroqService.ts",
            "resumo_executivo · indicadores · recomendacoes",
            "Relatório IA — Surto respiratório",
            new Dictionary<string, string> { ["indicator"] = "Indicador", ["value"] = "Valor" }),

        ["er.visits.by-triage"] = new(
            "dev-queiroz/sistema-hospitalar",
            "src/modulo/triagem/model/Triagem.ts",
            "Distribuição por nivel_gravidade",
            "Relatório operacional — Triagens PS",
            new Dictionary<string, string> { ["urgency"] = "Nível de gravidade", ["count"] = "Triagens (n)" }),

        ["er.wait.by-triage"] = new(
            "dev-queiroz/sistema-hospitalar",
            "src/modulo/triagem/service/TriagemService.ts",
            "Tempo médio de espera por classificação",
            "Relatório operacional — Fila PS",
            new Dictionary<string, string>
            {
                ["urgency"] = "Nível de gravidade",
                ["avgMinutes"] = "Tempo médio de espera (min)",
            }),

        ["er.patients.served"] = new(
            "dev-queiroz/sistema-hospitalar",
            "src/utils/pdfCompiler.ts — Triagens Associadas",
            "Pacientes atendidos no PS",
            "Relatório clínico — PS",
            new Dictionary<string, string>
            {
                ["arrivedAt"] = "Data de chegada",
                ["patient"] = "Paciente",
                ["urgency"] = "Gravidade",
                ["status"] = "Situação do atendimento",
            }),

        ["reg.ciha"] = new(
            "DATASUS/SIA-SUS", "Produção CIHA",
            "Controle de Internação Hospitalar de Alta Complexidade Ambulatorial",
            "Relatório Regulatório — CIHA",
            new Dictionary<string, string>
            {
                ["date"] = "Data do atendimento", ["patient"] = "Paciente", ["cns"] = "CNS",
                ["procedure"] = "Procedimento", ["protocol"] = "Protocolo / Máquina",
                ["cycle"] = "Ciclo", ["status"] = "Situação", ["sigtap"] = "Código SIGTAP",
            }),

        ["reg.apac"] = new(
            "DATASUS/SIA-SUS", "Autorização APAC",
            "APAC — oncologia e diálise",
            "Relatório Regulatório — APAC",
            new Dictionary<string, string>
            {
                ["apac"] = "Nº APAC", ["date"] = "Data", ["patient"] = "Paciente", ["cns"] = "CNS",
                ["procedure"] = "Procedimento SIGTAP", ["label"] = "Descrição do procedimento",
                ["cid"] = "CID-10", ["professional"] = "Profissional responsável",
                ["validity"] = "Validade", ["status"] = "Situação",
            }),

        ["reg.bpa"] = new(
            "DATASUS/SIA-SUS", "Boletim de Produção Ambulatorial",
            "BPA — atendimentos ambulatoriais",
            "Relatório Regulatório — BPA",
            new Dictionary<string, string>
            {
                ["date"] = "Data", ["patient"] = "Paciente", ["cns"] = "CNS",
                ["professional"] = "Profissional", ["specialty"] = "Especialidade (CBO)",
                ["procedure"] = "Procedimento SIGTAP",
            }),

        ["reg.aih"] = new(
            "DATASUS/SIH-SUS", "Autorização de Internação Hospitalar",
            "AIH / SIH-SUS",
            "Relatório Regulatório — AIH",
            new Dictionary<string, string>
            {
                ["admitted"] = "Data internação", ["patient"] = "Paciente", ["cns"] = "CNS",
                ["aih"] = "Nº AIH", ["cid"] = "CID-10", ["procedure"] = "SIGTAP",
                ["competence"] = "Competência", ["ward"] = "Ala / Setor",
            }),

        ["reg.compulsory-notifications"] = new(
            "R4EPI/sitrep", "OpenHospital notification diseases",
            "Doenças de notificação compulsória",
            "Relatório Regulatório — NOTIFICA",
            new Dictionary<string, string>
            {
                ["code"] = "Código", ["disease"] = "Doença / Agravos",
                ["cases"] = "Suspeitas/registros (n)", ["lastCase"] = "Último registro",
            }),

        ["pharmacy.abc-curve"] = new(
            "HospitalRun/hospitalrun-frontend", "inventory ABC",
            "Curva ABC — Farmácia", "Relatório gerencial — Curva ABC",
            new Dictionary<string, string>
            {
                ["product"] = "Produto", ["sku"] = "SKU", ["qty"] = "Quantidade consumida",
                ["value"] = "Valor (R$)", ["pct"] = "% individual",
                ["cumulative"] = "% acumulado", ["class"] = "Classe ABC",
            }),

        ["supply.abc-curve"] = new(
            "HospitalRun/hospitalrun-frontend", "inventory ABC",
            "Curva ABC — Almoxarifado", "Relatório gerencial — Curva ABC",
            new Dictionary<string, string>
            {
                ["product"] = "Produto", ["sku"] = "SKU", ["qty"] = "Quantidade consumida",
                ["value"] = "Valor (R$)", ["pct"] = "% individual",
                ["cumulative"] = "% acumulado", ["class"] = "Classe ABC",
            }),

        ["quality.adverse-events"] = new(
            "HospitalRun/hospitalrun-frontend", "src/incidents/",
            "Eventos adversos clínicos",
            "Relatório de Segurança — Eventos adversos",
            new Dictionary<string, string>
            {
                ["date"] = "Data do evento", ["type"] = "Tipo de incidente",
                ["location"] = "Local", ["severity"] = "Gravidade", ["description"] = "Descrição",
            }),

        ["quality.patient-falls"] = new(
            "HospitalRun/hospitalrun-frontend", "Patient fall incidents",
            "Vigilância de quedas de pacientes",
            "Relatório de Segurança — Quedas",
            new Dictionary<string, string>
            {
                ["date"] = "Data", ["patient"] = "Paciente", ["location"] = "Local",
                ["severity"] = "Gravidade", ["description"] = "Descrição",
            }),
    };

    public static FieldMapping? Get(string code)
    {
        if (ExactMap.TryGetValue(code, out var exact))
        {
            return exact;
        }

        var prefix = PrefixMap.FirstOrDefault(p => code.StartsWith(p.Key, StringComparison.OrdinalIgnoreCase));
        if (prefix.Value is null)
        {
            return null;
        }

        return new FieldMapping(
            prefix.Value.SourceRepo,
            prefix.Value.SourceTemplate,
            null,
            prefix.Value.DocumentType,
            prefix.Value.ColumnOverrides ?? new Dictionary<string, string>());
    }

    private static string ResolveColumnLabel(string code, string key, string fallback)
    {
        if (ExactMap.TryGetValue(code, out var exact)
            && exact.ColumnLabels.TryGetValue(key, out var exactLabel))
        {
            return exactLabel;
        }

        var prefix = PrefixMap.FirstOrDefault(p => code.StartsWith(p.Key, StringComparison.OrdinalIgnoreCase));
        if (prefix.Value?.ColumnOverrides?.TryGetValue(key, out var prefixLabel) == true)
        {
            return prefixLabel;
        }

        if (CommonColumnLabels.TryGetValue(key, out var common))
        {
            return common;
        }

        return fallback;
    }

    public static IReadOnlyList<ReportColumnDto> ApplyColumns(
        string code,
        IReadOnlyList<ReportColumnDto> columns) =>
        columns
            .Select(c => new ReportColumnDto(c.Key, ResolveColumnLabel(code, c.Key, c.Label), c.Format))
            .ToList();

    public static IReadOnlyList<ReportKpiDto> ApplyKpis(
        string code,
        IReadOnlyList<ReportKpiDto> kpis)
    {
        if (!ExactMap.TryGetValue(code, out var mapping) || mapping.KpiLabels is null)
        {
            return kpis;
        }

        return kpis
            .Select(k => new ReportKpiDto(
                mapping.KpiLabels.TryGetValue(k.Label, out var label) ? label : k.Label,
                k.Value,
                k.Variant))
            .ToList();
    }

    public static string? ResolveSubtitle(string code, string? subtitle)
    {
        if (ExactMap.TryGetValue(code, out var exact) && exact.PrintSubtitle is not null)
        {
            return string.IsNullOrWhiteSpace(subtitle)
                ? exact.PrintSubtitle
                : $"{exact.PrintSubtitle} · {subtitle}";
        }

        return subtitle;
    }

    public static string? ResolveDocumentType(string code, string? fallback)
    {
        var mapping = Get(code);
        return mapping?.DocumentType ?? fallback;
    }

    public static string? ResolveSourceTemplate(string code) => Get(code)?.SourceTemplate;
}
