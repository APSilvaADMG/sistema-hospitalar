using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence;

/// <summary>
/// Pendências de demonstração para o Centro de Pendências (estoque, TISS, assinatura).
/// Idempotente — marcador <see cref="DemoMarker"/>.
/// </summary>
public static class PendencyDemoSeed
{
    public const string DemoMarker = "gth-pendency-demo-v1";

    public static async Task EnsureAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await db.PendingItems.AnyAsync(
                p => p.IsActive && p.Descricao.Contains(DemoMarker),
                cancellationToken))
        {
            return;
        }

        var admin = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == "admin@hospital.local" && u.IsActive, cancellationToken);
        if (admin is null)
        {
            logger.LogWarning("Usuário admin não encontrado — seed de pendências ignorado.");
            return;
        }

        logger.LogInformation("Aplicando pendências de demonstração ({Marker})...", DemoMarker);

        var now = DateTime.UtcNow;
        db.PendingItems.AddRange(
            new PendingItem
            {
                UsuarioResponsavelId = admin.Id,
                Titulo = "Estoque baixo: Soro fisiológico 0,9%",
                Descricao = $"Saldo abaixo do mínimo — solicitar reposição urgente. {DemoMarker}",
                Modulo = PendingModule.Inventory,
                Tipo = PendingItemType.LowStock,
                Status = PendingItemStatus.Aberta,
                Prioridade = PendingItemPriority.Alta,
                Setor = "Almoxarifado Central",
                DataAbertura = now.AddHours(-6),
                LinkDestino = "/estoque/dashboard",
            },
            new PendingItem
            {
                UsuarioResponsavelId = admin.Id,
                Titulo = "Guia TISS em rascunho",
                Descricao = $"Guia de SP/SADT aguardando finalização e envio ao operador. {DemoMarker}",
                Modulo = PendingModule.Guides,
                Tipo = PendingItemType.GuideDraft,
                Status = PendingItemStatus.Aberta,
                Prioridade = PendingItemPriority.Normal,
                Setor = "Faturamento",
                DataAbertura = now.AddDays(-1),
                DataLimite = now.AddDays(2),
                LinkDestino = "/faturamento-tiss",
            },
            new PendingItem
            {
                UsuarioResponsavelId = admin.Id,
                Titulo = "Assinatura eletrônica pendente",
                Descricao = $"Prescrição médica aguardando assinatura com senha. {DemoMarker}",
                Modulo = PendingModule.System,
                Tipo = PendingItemType.WorkflowPending,
                Status = PendingItemStatus.Aberta,
                Prioridade = PendingItemPriority.Critica,
                Setor = "Pronto Socorro",
                DataAbertura = now.AddHours(-2),
                DataLimite = now.AddHours(4),
                LinkDestino = "/pep/assinaturas",
            });

        await db.SaveChangesAsync(cancellationToken);
    }
}
