using Microsoft.EntityFrameworkCore;
using SistemaHospitalar.Application.DTOs.Connect;
using SistemaHospitalar.Application.Interfaces;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Infrastructure.Persistence;

namespace SistemaHospitalar.Infrastructure.Connect;

public class BulletinService(AppDbContext db) : IBulletinService
{
    public async Task<IReadOnlyList<BulletinPostDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var posts = await db.BulletinPosts
            .AsNoTracking()
            .Where(p => p.IsActive && p.DeletedAt == null && p.PublishedAt != null)
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.PublishedAt)
            .Take(100)
            .Include(p => p.Author)
            .Include(p => p.Views)
            .ToListAsync(cancellationToken);

        return posts.Select(p => Map(p, userId)).ToList();
    }

    public async Task<BulletinPostDto?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var post = await db.BulletinPosts
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Views)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive && p.DeletedAt == null, cancellationToken);

        return post is null ? null : Map(post, userId);
    }

    public async Task<BulletinPostDto> CreateAsync(
        Guid userId, CreateBulletinPostRequest request, CancellationToken cancellationToken = default)
    {
        var post = new BulletinPost
        {
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            AuthorId = userId,
            IsPinned = request.IsPinned,
            PublishedAt = request.PublishNow ? DateTime.UtcNow : null,
        };

        db.BulletinPosts.Add(post);
        db.CommunicationAuditLogs.Add(new CommunicationAuditLog
        {
            UserId = userId,
            Action = "bulletin.create",
            EntityType = nameof(BulletinPost),
            EntityId = post.Id,
        });

        await db.SaveChangesAsync(cancellationToken);
        return Map(post, userId);
    }

    public async Task<BulletinPostDto?> UpdateAsync(
        Guid userId, Guid id, UpdateBulletinPostRequest request, CancellationToken cancellationToken = default)
    {
        var post = await db.BulletinPosts
            .Include(p => p.Author)
            .Include(p => p.Views)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive && p.DeletedAt == null, cancellationToken);

        if (post is null) return null;

        post.Title = request.Title.Trim();
        post.Content = request.Content.Trim();
        post.IsPinned = request.IsPinned;
        if (request.PublishNow && post.PublishedAt is null)
            post.PublishedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Map(post, userId);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var post = await db.BulletinPosts
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive && p.DeletedAt == null, cancellationToken);

        if (post is null) return false;

        post.DeletedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarkViewedAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await db.BulletinPosts.AnyAsync(
            p => p.Id == id && p.IsActive && p.DeletedAt == null && p.PublishedAt != null,
            cancellationToken);

        if (!exists) return false;

        if (await db.BulletinViews.AnyAsync(v => v.BulletinId == id && v.UserId == userId, cancellationToken))
            return true;

        db.BulletinViews.Add(new BulletinView
        {
            BulletinId = id,
            UserId = userId,
            ViewedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> GetUnviewedCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var publishedIds = await db.BulletinPosts
            .AsNoTracking()
            .Where(p => p.IsActive && p.DeletedAt == null && p.PublishedAt != null)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var viewedIds = await db.BulletinViews
            .AsNoTracking()
            .Where(v => v.UserId == userId && publishedIds.Contains(v.BulletinId))
            .Select(v => v.BulletinId)
            .ToListAsync(cancellationToken);

        return publishedIds.Count(id => !viewedIds.Contains(id));
    }

    private static BulletinPostDto Map(BulletinPost post, Guid userId) =>
        new(
            post.Id,
            post.Title,
            post.Content,
            post.Author.FullName,
            post.PublishedAt,
            post.IsPinned,
            post.Views.Any(v => v.UserId == userId),
            post.Views.Count,
            post.CreatedAt);
}
