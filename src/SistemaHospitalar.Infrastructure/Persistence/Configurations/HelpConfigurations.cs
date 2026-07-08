using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class HelpCategoryConfiguration : IEntityTypeConfiguration<HelpCategory>
{
    public void Configure(EntityTypeBuilder<HelpCategory> builder)
    {
        builder.ToTable("help_categories");
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(32);
    }
}

public class HelpArticleConfiguration : IEntityTypeConfiguration<HelpArticle>
{
    public void Configure(EntityTypeBuilder<HelpArticle> builder)
    {
        builder.ToTable("help_articles");
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.Slug).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(512);
        builder.Property(x => x.VideoUrl).HasMaxLength(512);
        builder.Property(x => x.DownloadUrl).HasMaxLength(512);
        builder.Property(x => x.Keywords).HasMaxLength(512);
        builder.Property(x => x.ContextRoutes).HasMaxLength(512);
        builder.HasOne(x => x.Category).WithMany(x => x.Articles).HasForeignKey(x => x.CategoryId);
    }
}

public class HelpArticleViewConfiguration : IEntityTypeConfiguration<HelpArticleView>
{
    public void Configure(EntityTypeBuilder<HelpArticleView> builder)
    {
        builder.ToTable("help_article_views");
        builder.HasOne(x => x.Article).WithMany(x => x.Views).HasForeignKey(x => x.ArticleId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class HelpTrainingProgressConfiguration : IEntityTypeConfiguration<HelpTrainingProgress>
{
    public void Configure(EntityTypeBuilder<HelpTrainingProgress> builder)
    {
        builder.ToTable("help_training_progress");
        builder.HasIndex(x => new { x.UserId, x.ArticleId }).IsUnique();
        builder.HasOne(x => x.Article).WithMany(x => x.TrainingProgress).HasForeignKey(x => x.ArticleId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

public class HelpSuggestionConfiguration : IEntityTypeConfiguration<HelpSuggestion>
{
    public void Configure(EntityTypeBuilder<HelpSuggestion> builder)
    {
        builder.ToTable("help_suggestions");
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(64);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}
