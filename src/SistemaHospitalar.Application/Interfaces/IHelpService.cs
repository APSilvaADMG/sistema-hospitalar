using SistemaHospitalar.Application.DTOs.Help;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Application.Interfaces;

public interface IHelpService
{
    Task<HelpSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HelpCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<HelpSearchResultDto> SearchAsync(string? query, HelpArticleType? type, string? categoryCode, int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HelpArticleListItemDto>> ListArticlesAsync(Guid? userId, HelpArticleType? type, string? categoryCode, CancellationToken cancellationToken = default);
    Task<HelpArticleDetailDto?> GetArticleAsync(Guid? userId, string slug, bool trackView, CancellationToken cancellationToken = default);
    Task<HelpContextDto> GetContextAsync(Guid? userId, string route, CancellationToken cancellationToken = default);
    Task<HelpAskResponse> AskAsync(Guid userId, HelpAskRequest request, CancellationToken cancellationToken = default);
    Task<HelpSuggestionDto> CreateSuggestionAsync(Guid userId, CreateHelpSuggestionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HelpSuggestionDto>> ListMySuggestionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task MarkTrainingCompleteAsync(Guid userId, Guid articleId, CancellationToken cancellationToken = default);
}
