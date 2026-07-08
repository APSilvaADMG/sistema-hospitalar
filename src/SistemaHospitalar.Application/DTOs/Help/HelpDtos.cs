using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.DTOs.Help;

public record HelpSummaryDto(
    int TotalArticles,
    int TotalFaqs,
    int TotalVideos,
    int TotalTrainings,
    int TotalManuals,
    int OpenTickets,
    int MyOpenTickets,
    int TotalViews,
    int PendingSuggestions);

public record HelpCategoryDto(
    Guid Id,
    string Code,
    string Name,
    string? Icon,
    int ArticleCount);

public record HelpArticleListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string? Summary,
    HelpArticleType Type,
    string CategoryCode,
    string CategoryName,
    int ViewCount,
    bool TrainingCompleted);

public record HelpArticleDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string? Summary,
    string Content,
    HelpArticleType Type,
    string CategoryCode,
    string CategoryName,
    string? VideoUrl,
    string? DownloadUrl,
    int ViewCount,
    bool TrainingCompleted);

public record HelpSearchResultDto(
    IReadOnlyList<HelpArticleListItemDto> Items,
    int Total);

public record HelpContextDto(
    string Route,
    string? ModuleLabel,
    IReadOnlyList<HelpArticleListItemDto> Articles,
    IReadOnlyList<HelpArticleListItemDto> Faqs);

public record HelpAskRequest(string Question, string? Route);

public record HelpAskResponse(
    string Question,
    string Answer,
    IReadOnlyList<HelpArticleListItemDto> RelatedArticles);

public record CreateHelpSuggestionRequest(string Title, string Description, string? Module);

public record HelpSuggestionDto(
    Guid Id,
    string Title,
    string Description,
    string? Module,
    HelpSuggestionStatus Status,
    DateTime CreatedAt);

public record MarkTrainingCompleteRequest(Guid ArticleId);
