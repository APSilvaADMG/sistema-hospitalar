using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class Cid10CatalogSeed
{
    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Cid10Catalogs
            .Select(c => c.Code)
            .ToListAsync(cancellationToken);
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = GetCatalogEntries()
            .Where(c => !existingSet.Contains(c.Code))
            .ToList();

        if (toAdd.Count == 0)
        {
            return;
        }

        dbContext.Cid10Catalogs.AddRange(toAdd);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static IReadOnlyList<Cid10Catalog> GetCatalogEntries() =>
    [
        new() { Code = "J06.9", Description = "Infecção aguda das vias aéreas superiores, NE", Category = "Respiratório", Keywords = "tosse coriza febre gripe resfriado ivas" },
        new() { Code = "J18.9", Description = "Pneumonia, não especificada", Category = "Respiratório", Keywords = "pneumonia pulmão infecção respiratória dispneia" },
        new() { Code = "J45.9", Description = "Asma, não especificada", Category = "Respiratório", Keywords = "asma broncoespasmo sibilância falta de ar" },
        new() { Code = "J44.9", Description = "Doença pulmonar obstrutiva crônica, NE", Category = "Respiratório", Keywords = "dpoc enfisema bronquite crônica" },
        new() { Code = "R50.9", Description = "Febre, não especificada", Category = "Sintomas", Keywords = "febre calafrio temperatura hipertermia" },
        new() { Code = "R51", Description = "Cefaleia", Category = "Sintomas", Keywords = "dor de cabeça cefaleia enxaqueca" },
        new() { Code = "R10.4", Description = "Dor abdominal e pélvica", Category = "Sintomas", Keywords = "dor abdominal barriga cólica epigástrio" },
        new() { Code = "R11", Description = "Náusea e vômito", Category = "Sintomas", Keywords = "vômito náusea enjoo êmese" },
        new() { Code = "R05", Description = "Tosse", Category = "Sintomas", Keywords = "tosse seca produtiva" },
        new() { Code = "R06.0", Description = "Dispneia", Category = "Sintomas", Keywords = "falta de ar dispneia cansaço" },
        new() { Code = "R07.4", Description = "Dor torácica, não especificada", Category = "Sintomas", Keywords = "dor torácica peito" },
        new() { Code = "I10", Description = "Hipertensão essencial (primária)", Category = "Cardiovascular", Keywords = "hipertensão pressão alta has" },
        new() { Code = "I20.9", Description = "Angina pectoris, não especificada", Category = "Cardiovascular", Keywords = "dor no peito angina isquemia" },
        new() { Code = "I21.9", Description = "Infarto agudo do miocárdio, NE", Category = "Cardiovascular", Keywords = "infarto iam sca dor precordial" },
        new() { Code = "I50.9", Description = "Insuficiência cardíaca, NE", Category = "Cardiovascular", Keywords = "ic insuficiência cardíaca edema dispneia" },
        new() { Code = "I48", Description = "Fibrilação e flutter atrial", Category = "Cardiovascular", Keywords = "fa fibrilação atrial arritmia" },
        new() { Code = "E11.9", Description = "Diabetes mellitus tipo 2, sem complicações", Category = "Endócrino", Keywords = "diabetes dm2 glicemia hiperglicemia" },
        new() { Code = "E78.5", Description = "Hiperlipidemia, NE", Category = "Endócrino", Keywords = "colesterol triglicerídeos dislipidemia" },
        new() { Code = "N39.0", Description = "Infecção do trato urinário, local não especificada", Category = "Urológico", Keywords = "itu infecção urinária disúria" },
        new() { Code = "K29.7", Description = "Gastrite, não especificada", Category = "Digestivo", Keywords = "gastrite epigastralgia azia" },
        new() { Code = "K59.0", Description = "Constipação", Category = "Digestivo", Keywords = "constipação prisão de ventre" },
        new() { Code = "K92.2", Description = "Hemorragia gastrointestinal, NE", Category = "Digestivo", Keywords = "hemorragia digestiva melena hematêmese" },
        new() { Code = "M54.5", Description = "Dor lombar baixa", Category = "Ortopedia", Keywords = "lombalgia dor lombar coluna" },
        new() { Code = "S06.0", Description = "Concussão cerebral", Category = "Trauma", Keywords = "tce trauma craniano concussão" },
        new() { Code = "T14.9", Description = "Traumatismo, não especificado", Category = "Trauma", Keywords = "trauma acidente contusão" },
        new() { Code = "F41.9", Description = "Transtorno de ansiedade, NE", Category = "Psiquiatria", Keywords = "ansiedade nervosismo pânico" },
        new() { Code = "F32.9", Description = "Episódio depressivo, NE", Category = "Psiquiatria", Keywords = "depressão humor tristeza" },
        new() { Code = "G43.9", Description = "Enxaqueca, NE", Category = "Neurologia", Keywords = "enxaqueca migrânea cefaleia" },
        new() { Code = "G40.9", Description = "Epilepsia, NE", Category = "Neurologia", Keywords = "epilepsia convulsão crise" },
        new() { Code = "B34.9", Description = "Infecção viral, NE", Category = "Infeccioso", Keywords = "vírus infecção viral" },
        new() { Code = "A09", Description = "Diarreia e gastroenterite de origem infecciosa presumível", Category = "Infeccioso", Keywords = "diarreia gastroenterite vômito" },
        new() { Code = "L30.9", Description = "Dermatite, NE", Category = "Dermatologia", Keywords = "dermatite pele rash eritema" },
        new() { Code = "H10.9", Description = "Conjuntivite, NE", Category = "Oftalmologia", Keywords = "conjuntivite olho vermelho" },
        new() { Code = "O80", Description = "Parto único espontâneo", Category = "Obstetrícia", Keywords = "parto trabalho de parto" },
        new() { Code = "Z00.0", Description = "Exame médico geral", Category = "Preventivo", Keywords = "check-up rotina exame preventivo" },
        new() { Code = "Z23", Description = "Necessidade de imunização contra doença infecciosa", Category = "Preventivo", Keywords = "vacina imunização" },
        new() { Code = "Z51.1", Description = "Sessão de quimioterapia para neoplasia", Category = "Oncologia", Keywords = "quimioterapia oncologia" },
        new() { Code = "C50.9", Description = "Neoplasia maligna da mama, NE", Category = "Oncologia", Keywords = "câncer mama neoplasia" },
        new() { Code = "Z38.0", Description = "Recém-nascido único, nascido em hospital", Category = "Pediatria", Keywords = "recém-nascido rn neonato" },
        new() { Code = "J00", Description = "Nasofaringite aguda (resfriado comum)", Category = "Respiratório", Keywords = "resfriado coriza tosse" },
    ];
}
