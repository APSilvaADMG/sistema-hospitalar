using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class BillingCatalogSeed
{
    public static IReadOnlyList<CbhpmProcedure> CbhpmItems { get; } =
    [
        Cbhpm("31001017", "Apendicectomia", "5A", 1850m),
        Cbhpm("31003086", "Colecistectomia videolaparoscópica", "6B", 2400m),
        Cbhpm("31005470", "Herniorrafia inguinal unilateral", "4A", 980m),
        Cbhpm("30901014", "Consulta em consultório", "1A", 95m),
        Cbhpm("30901103", "Consulta em pronto-socorro", "1B", 125m),
        Cbhpm("40801063", "Anestesia geral", "4B", 420m),
        Cbhpm("40301010", "Hemograma completo", "2A", 12m),
        Cbhpm("40801144", "Radiografia de tórax PA/perfil", "2B", 28m),
        Cbhpm("41001010", "Tomografia computadorizada crânio", "6A", 520m),
        Cbhpm("41101014", "Ressonância magnética joelho", "7A", 780m),
    ];

    public static IReadOnlyList<BrasindiceItem> BrasindiceItems { get; } =
    [
        Bras("0001234567", "Dipirona 500mg comp", "Medley", "cx 20", 18.50m),
        Bras("0002345678", "Omeprazol 20mg comp", "Eurofarma", "cx 28", 32.90m),
        Bras("0003456789", "Insulina NPH 100UI/ml", "Novo Nordisk", "fr 10ml", 89.00m),
        Bras("0004567890", "Soro fisiológico 0,9% 500ml", "Baxter", "bolsa", 12.40m),
        Bras("0005678901", "Ceftriaxona 1g pó", "Aché", "fr-amp", 28.70m),
        Bras("0006789012", "Morfina 10mg/ml amp", "Cristália", "amp 1ml", 15.80m),
        Bras("0007890123", "Heparina 5000UI/ml", "Sanofi", "fr-amp", 22.30m),
        Bras("0008901234", "Adrenalina 1mg/ml amp", "Hipolabor", "amp 1ml", 8.90m),
    ];

    public static IReadOnlyList<SimproItem> SimproItems { get; } =
    [
        Simpro("10001234", "Luva procedimento M", "Descarpack", "PAR", 0.85m),
        Simpro("10002345", "Seringa descartável 10ml", "BD", "UN", 1.20m),
        Simpro("10003456", "Cateter venoso 18G", "Prodimed", "UN", 4.50m),
        Simpro("10004567", "Máscara cirúrgica tripla", "Medix", "UN", 1.80m),
        Simpro("10005678", "Campo cirúrgico fenestrado", "Descarpack", "UN", 12.00m),
        Simpro("10006789", "Stent coronariano farmacológico", "Abbott", "UN", 8500m),
        Simpro("10007890", "Prótese total quadril cimento", "Johnson", "UN", 4200m),
        Simpro("10008901", "Marcapasso bicameral", "Medtronic", "UN", 18500m),
    ];

    private static CbhpmProcedure Cbhpm(string code, string desc, string port, decimal price) => new()
    {
        Code = code,
        Description = desc,
        Port = port,
        ReferencePrice = price,
    };

    private static BrasindiceItem Bras(string code, string desc, string lab, string presentation, decimal price) => new()
    {
        Code = code,
        Description = desc,
        Laboratory = lab,
        Presentation = presentation,
        ReferencePrice = price,
    };

    private static SimproItem Simpro(string code, string desc, string manufacturer, string unit, decimal price) => new()
    {
        Code = code,
        Description = desc,
        Manufacturer = manufacturer,
        Unit = unit,
        ReferencePrice = price,
    };
}
