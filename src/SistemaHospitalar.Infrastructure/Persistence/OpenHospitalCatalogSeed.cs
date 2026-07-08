using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Catálogos inspirados no OpenHospital v1.15 (data_pt) — vacinas e doenças epidemiológicas.
/// </summary>
public static class OpenHospitalCatalogSeed
{
    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.VaccineCatalogs.AnyAsync(cancellationToken))
        {
            dbContext.VaccineCatalogs.AddRange(BuildVaccines());
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.EpidemicDiseaseCatalogs.AnyAsync(cancellationToken))
        {
            dbContext.EpidemicDiseaseCatalogs.AddRange(BuildDiseases());
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static IEnumerable<VaccineCatalog> BuildVaccines() =>
    [
        V("1", "BCG", VaccineScheduleType.Child, 1),
        V("2", "PÓLIO 0", VaccineScheduleType.Child, 2),
        V("3", "PÓLIO 1", VaccineScheduleType.Child, 3),
        V("4", "PÓLIO 2", VaccineScheduleType.Child, 4),
        V("5", "PÓLIO 3", VaccineScheduleType.Child, 5),
        V("6", "DPT 1 - VHB + Hib 1", VaccineScheduleType.Child, 6),
        V("7", "DPT 2 - VHB + Hib 2", VaccineScheduleType.Child, 7),
        V("8", "DPT 3 - VHB + Hib 3", VaccineScheduleType.Child, 8),
        V("9", "SARAMPO", VaccineScheduleType.Child, 9),
        V("10", "DOSE DE VACINA TT 1", VaccineScheduleType.Pregnant, 10),
        V("11", "DOSE DE VACINA TT 2", VaccineScheduleType.Pregnant, 11),
        V("12", "DOSE DE VACINA TT 3", VaccineScheduleType.Pregnant, 12),
        V("13", "DOSE DE VACINA TT 4", VaccineScheduleType.Pregnant, 13),
        V("14", "DOSE DE VACINA TT 5", VaccineScheduleType.Pregnant, 14),
        V("15", "DOSE DE VACINA TT 2", VaccineScheduleType.NonPregnantAdult, 15),
        V("16", "DOSE DE VACINA TT 3", VaccineScheduleType.NonPregnantAdult, 16),
        V("17", "DOSE DE VACINA TT 4", VaccineScheduleType.NonPregnantAdult, 17),
        V("18", "DOSE DE VACINA TT 5", VaccineScheduleType.NonPregnantAdult, 18),
    ];

    private static VaccineCatalog V(string code, string name, VaccineScheduleType type, int order) =>
        new() { Code = code, Name = name, ScheduleType = type, DisplayOrder = order };

    private static IEnumerable<EpidemicDiseaseCatalog> BuildDiseases()
    {
        var rows = new (string Code, string Name, string Class, bool Opd, bool Ipd)[]
        {
            ("1", "Paralisia flácida aguda", "ND", true, true),
            ("2", "Cólera", "ND", true, true),
            ("3", "Diarréia-disenteria", "ND", true, true),
            ("4", "Verme da Guiné", "ND", true, true),
            ("5", "Sarampo", "ND", true, true),
            ("6", "Meningite", "ND", true, true),
            ("7", "Tétano neonatal (menos a idade de 28 dias)", "ND", true, true),
            ("8", "Praga", "ND", true, true),
            ("9", "Raiva", "ND", true, true),
            ("10", "Febre hemorrágica", "ND", true, true),
            ("11", "Febre amarela", "ND", true, true),
            ("12", "Anemia", "NC", true, true),
            ("14", "Diabetes Mellitus", "NC", true, true),
            ("16", "Hipertensão", "NC", true, true),
            ("17", "Doença mental", "NC", true, true),
            ("18", "Epilepsia", "NC", true, true),
            ("26", "Outras complicações da gravidez", "MP", false, true),
            ("27", "Condições perinatais", "MP", true, true),
            ("28", "Abortos", "MP", true, true),
            ("29", "AIDS/SIDA", "OC", true, true),
            ("39", "Malária", "OC", true, true),
            ("42", "Pneumonia", "OC", true, true),
            ("44", "Tuberculose", "OC", true, true),
            ("47", "Infecções do trato urinário (ITU)", "OC", true, true),
            ("49", "Todas as outras doenças", "AO", true, true),
            ("50", "Outras doenças infecciosas emergentes", "ND", true, true),
            ("59", "Infecções sexualmente transmissíveis (IST)", "OC", true, true),
            ("60", "Hepatite", "OC", true, false),
            ("68", "Asma", "NC", true, true),
            ("94", "Câncer de mama", "NC", true, true),
            ("102", "Eventos cerebro-vasculares", "NC", true, true),
            ("116", "Malária na gravidez", "MP", true, true),
        };

        return rows.Select((row, index) => new EpidemicDiseaseCatalog
        {
            Code = row.Code,
            Name = row.Name,
            DiseaseClass = MapClass(row.Class),
            IncludeOpd = row.Opd,
            IncludeIpd = row.Ipd,
            DisplayOrder = index + 1,
        });
    }

    private static EpidemicDiseaseClass MapClass(string code) => code switch
    {
        "ND" => EpidemicDiseaseClass.Notifiable,
        "NC" => EpidemicDiseaseClass.Chronic,
        "MP" => EpidemicDiseaseClass.MaternalPerinatal,
        "OC" => EpidemicDiseaseClass.OtherCondition,
        _ => EpidemicDiseaseClass.Other,
    };
}
