using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Catálogo TUSS ampliado com procedimentos hospitalares mais utilizados (referência ANS).
/// Complementa o seed base; importação CSV pode substituir/ampliar via API.
/// </summary>
public static class TissCatalogExpandedSeed
{
    public static IReadOnlyList<TussCatalog> Items { get; } =
    [
        // Consultas
        P("10101012", "Consulta em consultório — clínica médica", TussTableType.Procedure, "UN", 250m),
        P("10101020", "Consulta em consultório — pediatria", TussTableType.Procedure, "UN", 280m),
        P("10101039", "Consulta em pronto-socorro", TussTableType.Procedure, "UN", 320m),
        P("10102019", "Visita hospitalar", TussTableType.Procedure, "UN", 180m),
        P("10103015", "Atendimento ao recém-nascido em berçário", TussTableType.Procedure, "UN", 220m),
        P("10104011", "Atendimento ambulatorial em especialidade", TussTableType.Procedure, "UN", 290m),
        P("10106014", "Consulta em domicílio", TussTableType.Procedure, "UN", 350m),

        // Laboratório
        P("40301010", "Hemograma completo", TussTableType.Procedure, "UN", 18m),
        P("40302040", "Glicemia em jejum", TussTableType.Procedure, "UN", 8m),
        P("40302750", "Hemoglobina glicada (HbA1c)", TussTableType.Procedure, "UN", 35m),
        P("40304361", "Hemograma com contagem de plaquetas", TussTableType.Procedure, "UN", 22m),
        P("40311210", "Ureia", TussTableType.Procedure, "UN", 10m),
        P("40311228", "Creatinina", TussTableType.Procedure, "UN", 10m),
        P("40316049", "TSH", TussTableType.Procedure, "UN", 22m),
        P("40316520", "T4 livre", TussTableType.Procedure, "UN", 28m),
        P("40322491", "Sódio", TussTableType.Procedure, "UN", 9m),
        P("40322505", "Potássio", TussTableType.Procedure, "UN", 9m),
        P("40323640", "PCR (proteína C reativa)", TussTableType.Procedure, "UN", 25m),
        P("40324030", "Urina tipo I (EAS)", TussTableType.Procedure, "UN", 12m),
        P("40324111", "Urocultura", TussTableType.Procedure, "UN", 28m),
        P("40403178", "PSA total", TussTableType.Procedure, "UN", 45m),

        // Imagem
        P("40801144", "Radiografia de tórax PA e perfil", TussTableType.Procedure, "UN", 45m),
        P("40805018", "Radiografia de abdome", TussTableType.Procedure, "UN", 55m),
        P("40901017", "Ecocardiograma transtorácico", TussTableType.Procedure, "UN", 180m),
        P("40901106", "Ultrassonografia abdominal total", TussTableType.Procedure, "UN", 95m),
        P("40901203", "Ultrassonografia pélvica", TussTableType.Procedure, "UN", 85m),
        P("41001010", "Tomografia computadorizada de crânio", TussTableType.Procedure, "UN", 380m),
        P("41001150", "Tomografia computadorizada de tórax", TussTableType.Procedure, "UN", 420m),
        P("41001240", "Tomografia computadorizada de abdome", TussTableType.Procedure, "UN", 450m),
        P("41101014", "Ressonância magnética de joelho", TussTableType.Procedure, "UN", 620m),
        P("41101111", "Ressonância magnética de coluna lombar", TussTableType.Procedure, "UN", 680m),
        P("41101219", "Ressonância magnética de crânio", TussTableType.Procedure, "UN", 720m),
        P("40808033", "Mamografia bilateral", TussTableType.Procedure, "UN", 120m),

        // Terapias / SADT
        P("20101015", "Sessão de fisioterapia", TussTableType.Procedure, "UN", 95m),
        P("20101023", "Sessão de fonoaudiologia", TussTableType.Procedure, "UN", 85m),
        P("20101031", "Sessão de terapia ocupacional", TussTableType.Procedure, "UN", 85m),
        P("20104046", "Sessão de hemodiálise", TussTableType.Procedure, "UN", 320m),
        P("20201015", "Sessão de quimioterapia ambulatorial", TussTableType.Procedure, "UN", 450m),

        // Internação / diárias
        P("60000651", "Diária de apartamento", TussTableType.Daily, "DIA", 800m),
        P("60000732", "Diária de enfermaria", TussTableType.Daily, "DIA", 450m),
        P("60000821", "Diária de UTI adulto", TussTableType.Daily, "DIA", 2200m),
        P("60000910", "Diária de berçário", TussTableType.Daily, "DIA", 380m),
        P("60001024", "Diária de isolamento", TussTableType.Daily, "DIA", 950m),

        // Taxas / salas
        P("80071272", "Taxa de sala cirúrgica porte 5", TussTableType.Fee, "UN", 1200m),
        P("80071370", "Taxa de sala cirúrgica porte 6", TussTableType.Fee, "UN", 1800m),
        P("80071469", "Taxa de sala cirúrgica porte 7", TussTableType.Fee, "UN", 2400m),
        P("80071567", "Taxa de recuperação pós-anestésica", TussTableType.Fee, "UN", 450m),

        // Cirurgias (CBHPM cruzado)
        P("31001017", "Apendicectomia", TussTableType.Procedure, "UN", 1850m),
        P("31003086", "Colecistectomia videolaparoscópica", TussTableType.Procedure, "UN", 2400m),
        P("31005470", "Herniorrafia inguinal", TussTableType.Procedure, "UN", 980m),
        P("30901014", "Consulta pré-anestésica", TussTableType.Procedure, "UN", 95m),
        P("40801063", "Anestesia geral", TussTableType.Procedure, "UN", 420m),

        // Materiais / medicamentos (TUSS)
        P("90000001", "Material descartável uso hospitalar", TussTableType.Material, "UN", 35m),
        P("90065890", "Stent coronariano farmacológico", TussTableType.Material, "UN", 8500m),
        P("90496672", "Pacote quimioterapia ambulatorial", TussTableType.Package, "UN", 2800m),
        P("90397870", "Seringa descartável 10ml", TussTableType.Material, "UN", 2m),
        P("90397960", "Equipo para soro", TussTableType.Material, "UN", 8m),

        // Odonto (GTO)
        P("81000030", "Consulta odontológica inicial", TussTableType.Procedure, "UN", 120m),
        P("81000340", "Restauração em resina", TussTableType.Procedure, "UN", 85m),
        P("81000430", "Exodontia simples", TussTableType.Procedure, "UN", 95m),
    ];

    public static string SampleCsvHeader =>
        "codigo;descricao;tipo;unidade;valor_referencia";

    public static string SampleCsvContent => string.Join('\n',
        [SampleCsvHeader, .. Items.Select(i =>
            $"{i.Code};{i.Description};{MapType(i.TableType)};{i.Unit};{i.ReferencePrice?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}")]);

    private static TussCatalog P(string code, string desc, TussTableType type, string unit, decimal price) => new()
    {
        Code = code,
        Description = desc,
        TableType = type,
        Unit = unit,
        ReferencePrice = price,
    };

    private static string MapType(TussTableType type) => type switch
    {
        TussTableType.Procedure => "procedimento",
        TussTableType.Material => "material",
        TussTableType.Medication => "medicamento",
        TussTableType.Daily => "diaria",
        TussTableType.Fee => "taxa",
        TussTableType.Package => "pacote",
        _ => "procedimento",
    };
}
