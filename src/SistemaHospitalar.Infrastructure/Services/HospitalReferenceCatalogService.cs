using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.HospitalCatalog;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Services;

public class HospitalReferenceCatalogService(AppDbContext dbContext) : IHospitalReferenceCatalogService
{
    private static readonly IReadOnlyDictionary<HospitalReferenceCatalogType, (string Label, string Description)> TypeMeta =
        new Dictionary<HospitalReferenceCatalogType, (string, string)>
        {
            [HospitalReferenceCatalogType.UserType] = ("Tipos de usuário", "Perfis funcionais administrativos, assistenciais, diagnósticos, apoio e financeiros."),
            [HospitalReferenceCatalogType.HospitalSector] = ("Setores hospitalares", "Departamentos e setores operacionais do hospital."),
            [HospitalReferenceCatalogType.Ward] = ("Alas", "Alas de internação e unidades especializadas."),
            [HospitalReferenceCatalogType.BedType] = ("Tipos de leito", "Classificação de leitos e acomodações."),
            [HospitalReferenceCatalogType.SupplierType] = ("Tipos de fornecedor", "Classificação de fornecedores por categoria."),
            [HospitalReferenceCatalogType.ProductType] = ("Tipos de produto", "Classificação de produtos e insumos."),
            [HospitalReferenceCatalogType.ServiceType] = ("Tipos de serviço", "Classificação de serviços hospitalares."),
            [HospitalReferenceCatalogType.MedicalSpecialty] = ("Especialidades médicas", "Especialidades e CBO de referência."),
            [HospitalReferenceCatalogType.LabExam] = ("Exames laboratoriais", "Catálogo de exames de laboratório por categoria."),
            [HospitalReferenceCatalogType.ImagingExam] = ("Exames de imagem", "Catálogo de exames de diagnóstico por imagem."),
            [HospitalReferenceCatalogType.TissGuideType] = ("Tipos de guia TISS", "Guias e anexos do padrão TISS/ANS."),
            [HospitalReferenceCatalogType.SystemMenu] = ("Módulos do menu", "Estrutura de navegação do sistema."),
            [HospitalReferenceCatalogType.PermissionAction] = ("Ações de permissão", "Ações CRUD e operacionais para controle de acesso."),
            [HospitalReferenceCatalogType.ReadyProfile] = ("Perfis prontos", "Perfis de acesso pré-configurados."),
            [HospitalReferenceCatalogType.RegulatoryBase] = ("Bases regulatórias", "Tabelas e normas oficiais (SIGTAP, TUSS, TISS, CID, etc.)."),
            [HospitalReferenceCatalogType.RecommendedModule] = ("Módulos recomendados", "Módulos funcionais sugeridos para implantação hospitalar."),
        };

    public IReadOnlyList<HospitalReferenceCatalogTypeInfoDto> GetCatalogTypes()
        => Enum.GetValues<HospitalReferenceCatalogType>()
            .Select(t =>
            {
                var meta = TypeMeta[t];
                return new HospitalReferenceCatalogTypeInfoDto(t, meta.Label, meta.Description);
            })
            .ToList();

    public async Task<IReadOnlyList<HospitalReferenceCatalogSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var counts = await dbContext.HospitalReferenceCatalogItems
            .AsNoTracking()
            .Where(i => i.IsActive)
            .GroupBy(i => i.CatalogType)
            .Select(g => new
            {
                CatalogType = g.Key,
                ItemCount = g.Count(),
                GroupCount = g.Select(i => i.ParentGroup).Distinct().Count(),
            })
            .ToListAsync(cancellationToken);

        return Enum.GetValues<HospitalReferenceCatalogType>()
            .Select(t =>
            {
                var row = counts.FirstOrDefault(c => c.CatalogType == t);
                var meta = TypeMeta[t];
                return new HospitalReferenceCatalogSummaryDto(
                    t,
                    meta.Label,
                    row?.ItemCount ?? 0,
                    row?.GroupCount ?? 0);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<HospitalReferenceCatalogItemDto>> GetByTypeAsync(
        HospitalReferenceCatalogType catalogType,
        string? parentGroup = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.HospitalReferenceCatalogItems
            .AsNoTracking()
            .Where(i => i.IsActive && i.CatalogType == catalogType);

        if (!string.IsNullOrWhiteSpace(parentGroup))
        {
            query = query.Where(i => i.ParentGroup == parentGroup);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(i =>
                i.Name.ToLower().Contains(term)
                || i.Code.ToLower().Contains(term)
                || (i.ParentGroup != null && i.ParentGroup.ToLower().Contains(term))
                || (i.Description != null && i.Description.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(i => i.ParentGroup)
            .ThenBy(i => i.DisplayOrder)
            .ThenBy(i => i.Name)
            .Select(i => new HospitalReferenceCatalogItemDto(
                i.Code,
                i.Name,
                i.CatalogType,
                i.ParentGroup,
                i.DisplayOrder,
                i.Description,
                i.MetadataJson))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HospitalReferenceCatalogGroupDto>> GetGroupsAsync(
        HospitalReferenceCatalogType catalogType,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.HospitalReferenceCatalogItems
            .AsNoTracking()
            .Where(i => i.IsActive && i.CatalogType == catalogType)
            .GroupBy(i => i.ParentGroup)
            .Select(g => new HospitalReferenceCatalogGroupDto(g.Key, g.Count()))
            .OrderBy(g => g.ParentGroup)
            .ToListAsync(cancellationToken);
    }
}
