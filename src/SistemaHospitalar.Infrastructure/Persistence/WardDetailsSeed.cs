using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

public static class WardDetailsSeed
{
    private record WardDef(
        string Name,
        string Code,
        string Floor,
        string Description,
        WardCoverageModality Modality,
        WardCategory Category,
        int BedCount,
        string BedPrefix);

    private static readonly WardDef[] DefaultWards =
    [
        new("Enfermaria SUS", "SUS-ENF", "2",
            "Ala de enfermaria exclusiva para pacientes do SUS — leitos coletivos.",
            WardCoverageModality.Sus, WardCategory.Enfermaria, 8, "SUS-E"),
        new("Enfermaria Convênio", "CONV-ENF", "2",
            "Enfermaria para planos de saúde e convênios (Unimed, Bradesco, etc.).",
            WardCoverageModality.Convenio, WardCategory.Enfermaria, 6, "CV-E"),
        new("Apartamento Particular", "PART-APT", "4",
            "Apartamentos para pacientes particulares — acomodação superior.",
            WardCoverageModality.Particular, WardCategory.Apartamento, 4, "PT-A"),
        new("Apartamento Convênio", "CONV-APT", "4",
            "Apartamentos para convênios com cobertura de acomodação apartamento.",
            WardCoverageModality.Convenio, WardCategory.Apartamento, 4, "CV-A"),
        new("UTI SUS", "SUS-UTI", "3",
            "Unidade de terapia intensiva para pacientes SUS.",
            WardCoverageModality.Sus, WardCategory.Uti, 3, "SUS-U"),
        new("UTI Convênio", "CONV-UTI", "3",
            "UTI dedicada a convênios e planos de saúde.",
            WardCoverageModality.Convenio, WardCategory.Uti, 2, "CV-U"),
        new("UTI Particular", "PART-UTI", "3",
            "UTI para pacientes particulares.",
            WardCoverageModality.Particular, WardCategory.Uti, 2, "PT-U"),
        new("Maternidade SUS", "SUS-MAT", "5",
            "Ala de maternidade para partos e internações obstétricas via SUS.",
            WardCoverageModality.Sus, WardCategory.Maternidade, 4, "SUS-M"),
        new("Pediatria Convênio", "CONV-PED", "2",
            "Enfermaria pediátrica para convênios.",
            WardCoverageModality.Convenio, WardCategory.Pediatrica, 4, "CV-P"),
    ];

    public static void SeedFresh(AppDbContext db)
    {
        foreach (var def in DefaultWards)
        {
            AddWardWithBeds(db, def);
        }
    }

    public static async Task ApplyAsync(AppDbContext db, CancellationToken ct = default)
    {
        var existingByCode = await db.Wards
            .Include(w => w.Beds)
            .ToDictionaryAsync(w => w.Code ?? w.Name, w => w, ct);

        foreach (var def in DefaultWards)
        {
            if (existingByCode.ContainsKey(def.Code))
            {
                continue;
            }

            var legacy = await db.Wards
                .Include(w => w.Beds)
                .FirstOrDefaultAsync(w => w.Name == def.Name, ct);

            if (legacy is not null)
            {
                UpdateWard(legacy, def);
                continue;
            }

            AddWardWithBeds(db, def);
        }

        var enfermariaA = await db.Wards.FirstOrDefaultAsync(w => w.Name == "Enfermaria A", ct);
        if (enfermariaA is not null && string.IsNullOrEmpty(enfermariaA.Code))
        {
            enfermariaA.Name = "Enfermaria Mista";
            enfermariaA.Code = "MIX-ENF";
            enfermariaA.CoverageModality = WardCoverageModality.Mixed;
            enfermariaA.Category = WardCategory.Enfermaria;
            enfermariaA.Description = "Enfermaria de uso misto (legado) — preferir alas específicas por modalidade.";
        }

        var utiLegacy = await db.Wards.FirstOrDefaultAsync(w => w.Name == "UTI" && w.Code == null, ct);
        if (utiLegacy is not null)
        {
            utiLegacy.Code = "MIX-UTI";
            utiLegacy.CoverageModality = WardCoverageModality.Mixed;
            utiLegacy.Category = WardCategory.Uti;
            utiLegacy.Description = "UTI mista (legado) — preferir UTI SUS, Convênio ou Particular.";
        }

        foreach (var ward in await db.Wards.Where(w => w.Code == null).ToListAsync(ct))
        {
            if (ward.Name.Contains("UTI", StringComparison.OrdinalIgnoreCase))
            {
                ward.Category = WardCategory.Uti;
                ward.CoverageModality = WardCoverageModality.Mixed;
            }
        }
    }

    private static void UpdateWard(Ward ward, WardDef def)
    {
        ward.Code = def.Code;
        ward.Floor = def.Floor;
        ward.Description = def.Description;
        ward.CoverageModality = def.Modality;
        ward.Category = def.Category;

        if (ward.Beds.Count >= def.BedCount)
        {
            return;
        }

        for (var i = ward.Beds.Count + 1; i <= def.BedCount; i++)
        {
            ward.Beds.Add(new Bed { BedNumber = $"{def.BedPrefix}{i:00}" });
        }
    }

    private static void AddWardWithBeds(AppDbContext db, WardDef def)
    {
        var ward = new Ward
        {
            Name = def.Name,
            Code = def.Code,
            Floor = def.Floor,
            Description = def.Description,
            CoverageModality = def.Modality,
            Category = def.Category,
        };

        for (var i = 1; i <= def.BedCount; i++)
        {
            ward.Beds.Add(new Bed { BedNumber = $"{def.BedPrefix}{i:00}" });
        }

        db.Wards.Add(ward);
    }
}
