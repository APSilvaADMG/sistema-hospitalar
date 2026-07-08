using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class HospitalizationSnippetSeed
{
    private static readonly string[] DefaultReasons =
    [
        "Insuficiência respiratória aguda",
        "Pneumonia adquirida na comunidade",
        "Exacerbação de insuficiência cardíaca",
        "Crise hipertensiva",
        "Dor abdominal aguda",
        "Infecção do trato urinário complicada",
        "Descompensação do diabetes mellitus",
        "AVC isquêmico agudo",
        "Trauma / politrauma",
        "Pré-operatório",
        "Sepse / choque séptico",
        "Insuficiência renal aguda",
        "Exacerbação de DPOC",
        "Sangramento digestivo",
        "Internação para investigação diagnóstica",
    ];

    private static readonly string[] DefaultDiagnoses =
    [
        "Pneumonia bacteriana",
        "Insuficiência cardíaca descompensada",
        "Infecção urinária",
        "Dor abdominal inespecífica",
        "Diabetes mellitus descompensado",
        "Hipertensão arterial sistêmica",
        "DPOC exacerbada",
        "Apendicite aguda",
        "AVC isquêmico",
        "Sepse de foco abdominal",
        "Insuficiência renal aguda",
        "Gastroenterite aguda",
        "Colecistite aguda",
        "Fratura de fêmur",
        "Anemia sintomática",
    ];

    public static async Task EnsureAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await EnsureTypeAsync(dbContext, HospitalizationSnippetType.Reason, DefaultReasons, cancellationToken);
        await EnsureTypeAsync(dbContext, HospitalizationSnippetType.Diagnosis, DefaultDiagnoses, cancellationToken);
    }

    private static async Task EnsureTypeAsync(
        AppDbContext dbContext,
        HospitalizationSnippetType type,
        IEnumerable<string> texts,
        CancellationToken cancellationToken)
    {
        foreach (var text in texts)
        {
            var normalized = Normalize(text);
            var exists = await dbContext.HospitalizationSnippets.AnyAsync(
                s => s.Type == type && s.NormalizedText == normalized,
                cancellationToken);

            if (!exists)
            {
                dbContext.HospitalizationSnippets.Add(new HospitalizationSnippet
                {
                    Type = type,
                    Text = text.Trim(),
                    NormalizedText = normalized,
                    UsageCount = 0
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    internal static string Normalize(string text) =>
        text.Trim().ToUpperInvariant();
}
