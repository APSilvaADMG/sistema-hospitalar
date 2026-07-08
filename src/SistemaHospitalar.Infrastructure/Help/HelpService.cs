using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Help;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Help;

public class HelpService(AppDbContext db, IConnectTicketService ticketService) : IHelpService
{
    public async Task<HelpSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var articles = await db.HelpArticles.AsNoTracking().Where(a => a.IsActive).ToListAsync(cancellationToken);
        var ticketSummary = await ticketService.GetSummaryAsync(userId, cancellationToken);
        var myOpen = await db.ConnectTickets.AsNoTracking()
            .CountAsync(t => t.IsActive && t.DeletedAt == null && t.SolicitanteId == userId
                && t.Status != ConnectTicketStatus.Resolvido && t.Status != ConnectTicketStatus.Cancelado, cancellationToken);
        var views = await db.HelpArticleViews.AsNoTracking().CountAsync(cancellationToken);
        var suggestions = await db.HelpSuggestions.AsNoTracking()
            .CountAsync(s => s.IsActive && s.Status == HelpSuggestionStatus.Pendente, cancellationToken);

        return new HelpSummaryDto(
            articles.Count,
            articles.Count(a => a.Type == HelpArticleType.Faq),
            articles.Count(a => a.Type == HelpArticleType.Video),
            articles.Count(a => a.Type == HelpArticleType.Training),
            articles.Count(a => a.Type == HelpArticleType.Manual),
            ticketSummary.TotalAbertos + ticketSummary.TotalEmAndamento + ticketSummary.TotalAguardando,
            myOpen,
            views,
            suggestions);
    }

    public async Task<IReadOnlyList<HelpCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await db.HelpCategories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new HelpCategoryDto(
                c.Id,
                c.Code,
                c.Name,
                c.Icon,
                c.Articles.Count(a => a.IsActive)))
            .ToListAsync(cancellationToken);
    }

    public async Task<HelpSearchResultDto> SearchAsync(
        string? query,
        HelpArticleType? type,
        string? categoryCode,
        int limit,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var q = BuildArticleQuery(type, categoryCode);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            q = q.Where(a =>
                a.Title.ToLower().Contains(term)
                || (a.Summary != null && a.Summary.ToLower().Contains(term))
                || a.Content.ToLower().Contains(term)
                || (a.Keywords != null && a.Keywords.ToLower().Contains(term)));
        }

        var total = await q.CountAsync(cancellationToken);
        var rows = await q.OrderBy(a => a.SortOrder).ThenBy(a => a.Title)
            .Take(limit)
            .ToListAsync(cancellationToken);
        var items = rows.Select(a => ToListItem(a, null)).ToList();

        return new HelpSearchResultDto(items, total);
    }

    public async Task<IReadOnlyList<HelpArticleListItemDto>> ListArticlesAsync(
        Guid? userId,
        HelpArticleType? type,
        string? categoryCode,
        CancellationToken cancellationToken = default)
    {
        var completedIds = userId.HasValue
            ? await db.HelpTrainingProgress.AsNoTracking()
                .Where(p => p.UserId == userId.Value)
                .Select(p => p.ArticleId)
                .ToHashSetAsync(cancellationToken)
            : [];

        var rows = await BuildArticleQuery(type, categoryCode)
            .OrderBy(a => a.SortOrder).ThenBy(a => a.Title)
            .ToListAsync(cancellationToken);

        var items = rows.Select(a => ToListItem(a, completedIds.Contains(a.Id))).ToList();
        return items;
    }

    public async Task<HelpArticleDetailDto?> GetArticleAsync(
        Guid? userId,
        string slug,
        bool trackView,
        CancellationToken cancellationToken = default)
    {
        var article = await db.HelpArticles
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.IsActive && a.Slug == slug, cancellationToken);
        if (article is null) return null;

        if (trackView)
        {
            article.ViewCount++;
            db.HelpArticleViews.Add(new HelpArticleView
            {
                ArticleId = article.Id,
                UserId = userId,
                ViewedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        var completed = userId.HasValue && await db.HelpTrainingProgress.AsNoTracking()
            .AnyAsync(p => p.UserId == userId.Value && p.ArticleId == article.Id, cancellationToken);

        return ToDetail(article, completed);
    }

    public async Task<HelpContextDto> GetContextAsync(Guid? userId, string route, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRoute(route);
        var all = await db.HelpArticles.AsNoTracking()
            .Include(a => a.Category)
            .Where(a => a.IsActive && a.ContextRoutes != null)
            .ToListAsync(cancellationToken);

        var matched = all.Where(a => RouteMatches(normalized, a.ContextRoutes!)).ToList();
        var completedIds = userId.HasValue
            ? await db.HelpTrainingProgress.AsNoTracking()
                .Where(p => p.UserId == userId.Value)
                .Select(p => p.ArticleId)
                .ToHashSetAsync(cancellationToken)
            : [];

        var articles = matched
            .Where(a => a.Type is HelpArticleType.Article or HelpArticleType.Manual or HelpArticleType.Video or HelpArticleType.Training)
            .OrderBy(a => a.SortOrder)
            .Select(a => ToListItem(a, completedIds.Contains(a.Id)))
            .ToList();

        var faqs = matched
            .Where(a => a.Type == HelpArticleType.Faq)
            .OrderBy(a => a.SortOrder)
            .Select(a => ToListItem(a, false))
            .ToList();

        return new HelpContextDto(normalized, ResolveModuleLabel(normalized), articles, faqs);
    }

    public async Task<HelpAskResponse> AskAsync(Guid userId, HelpAskRequest request, CancellationToken cancellationToken = default)
    {
        var question = request.Question?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(question))
        {
            return new HelpAskResponse(question, "Digite sua dúvida para buscar na base de ajuda.", []);
        }

        var search = await SearchAsync(question, null, null, 5, cancellationToken);
        if (search.Items.Count == 0)
        {
            var routeHint = !string.IsNullOrWhiteSpace(request.Route)
                ? await GetContextAsync(userId, request.Route!, cancellationToken)
                : null;
            if (routeHint?.Faqs.Count > 0)
            {
                var top = routeHint.Faqs[0];
                var detail = await GetArticleAsync(userId, top.Slug, false, cancellationToken);
                return new HelpAskResponse(
                    question,
                    detail?.Summary ?? detail?.Content ?? "Consulte a Central de Ajuda para mais informações.",
                    routeHint.Faqs.Take(3).ToList());
            }

            return new HelpAskResponse(
                question,
                "Não encontrei artigos sobre isso. Tente outros termos ou abra um chamado em Ajuda → Suporte.",
                []);
        }

        var best = search.Items[0];
        var bestDetail = await GetArticleAsync(userId, best.Slug, false, cancellationToken);
        var answer = bestDetail?.Summary ?? Truncate(bestDetail?.Content ?? best.Title, 400);
        return new HelpAskResponse(question, answer, search.Items);
    }

    public async Task<HelpSuggestionDto> CreateSuggestionAsync(
        Guid userId,
        CreateHelpSuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = new HelpSuggestion
        {
            UserId = userId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Module = string.IsNullOrWhiteSpace(request.Module) ? null : request.Module.Trim(),
            Status = HelpSuggestionStatus.Pendente,
        };
        db.HelpSuggestions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToSuggestionDto(entity);
    }

    public async Task<IReadOnlyList<HelpSuggestionDto>> ListMySuggestionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.HelpSuggestions.AsNoTracking()
            .Where(s => s.IsActive && s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(ToSuggestionDto).ToList();
    }

    public async Task MarkTrainingCompleteAsync(Guid userId, Guid articleId, CancellationToken cancellationToken = default)
    {
        var exists = await db.HelpTrainingProgress
            .AnyAsync(p => p.UserId == userId && p.ArticleId == articleId, cancellationToken);
        if (exists) return;

        db.HelpTrainingProgress.Add(new HelpTrainingProgress
        {
            UserId = userId,
            ArticleId = articleId,
            CompletedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<HelpArticle> BuildArticleQuery(HelpArticleType? type, string? categoryCode)
    {
        var q = db.HelpArticles.AsNoTracking().Include(a => a.Category).Where(a => a.IsActive);
        if (type.HasValue) q = q.Where(a => a.Type == type.Value);
        if (!string.IsNullOrWhiteSpace(categoryCode))
            q = q.Where(a => a.Category.Code == categoryCode);
        return q;
    }

    private static HelpArticleListItemDto ToListItem(HelpArticle a, bool? trainingCompleted) =>
        new(
            a.Id,
            a.Slug,
            a.Title,
            a.Summary,
            a.Type,
            a.Category.Code,
            a.Category.Name,
            a.ViewCount,
            trainingCompleted ?? false);

    private static HelpArticleDetailDto ToDetail(HelpArticle a, bool trainingCompleted) =>
        new(
            a.Id,
            a.Slug,
            a.Title,
            a.Summary,
            a.Content,
            a.Type,
            a.Category.Code,
            a.Category.Name,
            a.VideoUrl,
            a.DownloadUrl,
            a.ViewCount,
            trainingCompleted);

    private static HelpSuggestionDto ToSuggestionDto(HelpSuggestion s) =>
        new(s.Id, s.Title, s.Description, s.Module, s.Status, s.CreatedAt);

    private static string NormalizeRoute(string route)
    {
        var path = route.Split('?')[0].Trim().ToLowerInvariant();
        if (!path.StartsWith('/')) path = "/" + path;
        return path;
    }

    private static bool RouteMatches(string route, string contextRoutes)
    {
        foreach (var part in contextRoutes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var prefix = part.ToLowerInvariant();
            if (!prefix.StartsWith('/')) prefix = "/" + prefix;
            if (route == prefix || route.StartsWith(prefix + "/")) return true;
        }

        return false;
    }

    private static string? ResolveModuleLabel(string route)
    {
        if (route.StartsWith("/recepcao/pacientes") || route.StartsWith("/pacientes")) return "Pacientes";
        if (route.StartsWith("/guias") || route.StartsWith("/faturamento-tiss")) return "Guias e TISS";
        if (route.StartsWith("/faturamento/sus") || route.Contains("sigtap")) return "SUS / SIGTAP";
        if (route.StartsWith("/financeiro")) return "Financeiro";
        if (route.StartsWith("/recepcao/agendamentos") || route.Contains("agendamento")) return "Agenda";
        if (route.StartsWith("/internacao")) return "Internação";
        if (route.StartsWith("/estoque")) return "Estoque";
        if (route.StartsWith("/relatorios")) return "Relatórios";
        if (route.StartsWith("/configuracoes")) return "Configurações";
        if (route.StartsWith("/connect")) return "Connect";
        return null;
    }

    private static string Truncate(string text, int max)
    {
        if (text.Length <= max) return text;
        return text[..max].TrimEnd() + "…";
    }
}
