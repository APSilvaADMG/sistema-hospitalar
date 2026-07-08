using SistemaHospitalar.Application.DTOs.Tiss;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Tiss;

internal static class CatalogImportHelpers
{
    internal sealed class MergeStats
    {
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public int Changed => Inserted + Updated;
    }

    internal static IOrderedQueryable<TussCatalog> OrderByCatalogCode(IQueryable<TussCatalog> query)
        => query.OrderBy(t => t.Code.Length).ThenBy(t => t.Code);

    internal static IOrderedQueryable<SigtapProcedure> OrderByCatalogCode(IQueryable<SigtapProcedure> query)
        => query.OrderBy(s => s.Code.Length).ThenBy(s => s.Code);

    internal static bool TussContentEquals(TussCatalog existing, TussCatalogImportItem item, string description)
        => existing.Description == description
            && existing.TableType == item.TableType
            && string.Equals(NormalizeOptional(existing.Unit), NormalizeOptional(item.Unit), StringComparison.Ordinal)
            && existing.ReferencePrice == item.ReferencePrice
            && existing.IsActive;

    internal static void ApplyTussUpdate(TussCatalog existing, TussCatalogImportItem item, string description)
    {
        existing.Description = description;
        existing.TableType = item.TableType;
        existing.Unit = NormalizeOptional(item.Unit);
        existing.ReferencePrice = item.ReferencePrice;
        existing.IsActive = true;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    internal static bool SigtapContentEquals(
        SigtapProcedure existing,
        SigtapParsedItem item,
        string description,
        DateOnly? validFrom,
        DateOnly? validUntil)
        => existing.Description == description
            && string.Equals(NormalizeOptional(existing.GroupName), NormalizeOptional(item.GroupName), StringComparison.Ordinal)
            && string.Equals(NormalizeOptional(existing.Complexity), NormalizeOptional(item.Complexity), StringComparison.Ordinal)
            && existing.HospitalAmount == item.HospitalAmount
            && existing.ProfessionalAmount == item.ProfessionalAmount
            && existing.ValidFrom == validFrom
            && existing.ValidUntil == validUntil
            && existing.IsActive;

    internal static void ApplySigtapUpdate(
        SigtapProcedure existing,
        SigtapParsedItem item,
        string description,
        DateOnly? validFrom,
        DateOnly? validUntil)
    {
        existing.Description = description;
        existing.GroupName = NormalizeOptional(item.GroupName);
        existing.Complexity = NormalizeOptional(item.Complexity);
        existing.HospitalAmount = item.HospitalAmount;
        existing.ProfessionalAmount = item.ProfessionalAmount;
        existing.ValidFrom = validFrom;
        existing.ValidUntil = validUntil;
        existing.IsActive = true;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    internal static string FormatMergeMessage(string entityLabel, int totalInFile, MergeStats stats)
    {
        if (stats.Changed == 0)
            return $"{totalInFile} {entityLabel} lido(s); nenhuma alteração (todos já estavam atualizados).";

        if (stats.Skipped == 0)
            return $"{stats.Changed} {entityLabel} gravado(s) ({stats.Inserted} novo(s), {stats.Updated} atualizado(s)) de {totalInFile} lido(s).";

        return $"{stats.Changed} {entityLabel} gravado(s) ({stats.Inserted} novo(s), {stats.Updated} atualizado(s), {stats.Skipped} inalterado(s)) de {totalInFile} lido(s).";
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
