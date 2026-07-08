using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaHospitalar.Application.DTOs.Help;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Enums;
using System.Security.Claims;

namespace SistemaHospitalar.Api.Controllers;

[ApiController]
[Route("api/help")]
[Authorize]
public class HelpController(IHelpService helpService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await helpService.GetSummaryAsync(userId.Value, cancellationToken));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken) =>
        Ok(await helpService.GetCategoriesAsync(cancellationToken));

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] HelpArticleType? type,
        [FromQuery] string? category,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await helpService.SearchAsync(q, type, category, limit, cancellationToken));

    [HttpGet("articles")]
    public async Task<IActionResult> ListArticles(
        [FromQuery] HelpArticleType? type,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return Ok(await helpService.ListArticlesAsync(userId, type, category, cancellationToken));
    }

    [HttpGet("articles/{slug}")]
    public async Task<IActionResult> GetArticle(string slug, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var article = await helpService.GetArticleAsync(userId, slug, trackView: true, cancellationToken);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpGet("context")]
    public async Task<IActionResult> GetContext([FromQuery] string route, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return Ok(await helpService.GetContextAsync(userId, route, cancellationToken));
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] HelpAskRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await helpService.AskAsync(userId.Value, request, cancellationToken));
    }

    [HttpPost("suggestions")]
    public async Task<IActionResult> CreateSuggestion(
        [FromBody] CreateHelpSuggestionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await helpService.CreateSuggestionAsync(userId.Value, request, cancellationToken));
    }

    [HttpGet("suggestions/mine")]
    public async Task<IActionResult> ListMySuggestions(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await helpService.ListMySuggestionsAsync(userId.Value, cancellationToken));
    }

    [HttpPost("training/complete")]
    public async Task<IActionResult> MarkTrainingComplete(
        [FromBody] MarkTrainingCompleteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        await helpService.MarkTrainingCompleteAsync(userId.Value, request.ArticleId, cancellationToken);
        return NoContent();
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
