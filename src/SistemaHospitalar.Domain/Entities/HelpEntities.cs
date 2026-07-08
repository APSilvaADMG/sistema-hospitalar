using SistemaHospitalar.Domain.Common;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Domain.Entities;

public class HelpCategory : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }

    public ICollection<HelpArticle> Articles { get; set; } = [];
}

public class HelpArticle : BaseEntity
{
    public Guid CategoryId { get; set; }
    public HelpCategory Category { get; set; } = null!;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public HelpArticleType Type { get; set; } = HelpArticleType.Article;
    public string? VideoUrl { get; set; }
    public string? DownloadUrl { get; set; }
    public string? Keywords { get; set; }
    /// <summary>Prefixos de rota separados por vírgula para ajuda contextual.</summary>
    public string? ContextRoutes { get; set; }
    public int SortOrder { get; set; }
    public int ViewCount { get; set; }

    public ICollection<HelpArticleView> Views { get; set; } = [];
    public ICollection<HelpTrainingProgress> TrainingProgress { get; set; } = [];
}

public class HelpArticleView : BaseEntity
{
    public Guid ArticleId { get; set; }
    public HelpArticle Article { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
}

public class HelpTrainingProgress : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid ArticleId { get; set; }
    public HelpArticle Article { get; set; } = null!;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

public class HelpSuggestion : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Module { get; set; }
    public HelpSuggestionStatus Status { get; set; } = HelpSuggestionStatus.Pendente;
}
