using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class TissCatalogSeed
{
    public static IReadOnlyList<TussCatalog> Items { get; } =
    [
        Item("10101012", "Consulta em consultório", TussTableType.Procedure, "UN", 250m),
        Item("10101039", "Consulta em pronto-socorro", TussTableType.Procedure, "UN", 320m),
        Item("20101015", "Atendimento fisioterapia sessão", TussTableType.Procedure, "UN", 95m),
        Item("40301010", "Hemograma completo", TussTableType.Procedure, "UN", 18m),
        Item("40302040", "Glicemia em jejum", TussTableType.Procedure, "UN", 8m),
        Item("40316049", "TSH", TussTableType.Procedure, "UN", 22m),
        Item("40801144", "Raio-X tórax PA/perfil", TussTableType.Procedure, "UN", 45m),
        Item("41001010", "Tomografia computadorizada crânio", TussTableType.Procedure, "UN", 380m),
        Item("41101014", "Ressonância magnética joelho", TussTableType.Procedure, "UN", 620m),
        Item("60000651", "Diária apartamento", TussTableType.Daily, "DIA", 800m),
        Item("60000732", "Diária enfermaria", TussTableType.Daily, "DIA", 450m),
        Item("80071272", "Taxa de sala cirúrgica porte 5", TussTableType.Fee, "UN", 1200m),
        Item("90000001", "Material descartável uso hospitalar", TussTableType.Material, "UN", 35m),
        Item("90065890", "Stent coronariano farmacológico", TussTableType.Material, "UN", 8500m),
        Item("90496672", "Pacote quimioterapia ambulatorial", TussTableType.Package, "UN", 2800m),
    ];

    public static IReadOnlyList<SigtapProcedure> SigtapItems { get; } =
    [
        Sigtap("0301010070", "Consulta médica em atenção básica", "Atenção Básica", "MC", 10m, 0m),
        Sigtap("0301060060", "Consulta médica em atenção especializada", "Ambulatorial", "MC", 15m, 0m),
        Sigtap("0205020140", "Internação clínica adulto", "Hospitalar", "AC", 450m, 120m),
        Sigtap("0205020159", "Internação clínica pediátrica", "Hospitalar", "AC", 480m, 130m),
        Sigtap("0203020030", "Atendimento de urgência em PS", "Urgência", "MC", 85m, 45m),
        Sigtap("0301100010", "Hemograma completo", "Laboratório", "MC", 4m, 0m),
        Sigtap("0301100200", "Glicemia", "Laboratório", "MC", 2m, 0m),
        Sigtap("0204030188", "Radiografia de tórax PA/perfil", "Diagnóstico", "MC", 12m, 0m),
        Sigtap("0205020174", "Cirurgia apendicectomia", "Cirúrgico", "AC", 980m, 420m),
        Sigtap("0303140119", "Sessão hemodiálise", "Terapia", "AC", 320m, 80m),
    ];

    private static TussCatalog Item(string code, string desc, TussTableType type, string unit, decimal price) => new()
    {
        Code = code,
        Description = desc,
        TableType = type,
        Unit = unit,
        ReferencePrice = price,
    };

    private static SigtapProcedure Sigtap(string code, string desc, string group, string complexity, decimal hosp, decimal prof) => new()
    {
        Code = code,
        Competence = "2026-01",
        Description = desc,
        GroupName = group,
        Complexity = complexity,
        HospitalAmount = hosp,
        ProfessionalAmount = prof,
    };
}
