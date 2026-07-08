using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class TvDisplayConfiguration : IEntityTypeConfiguration<TvDisplay>
{
    public void Configure(EntityTypeBuilder<TvDisplay> builder)
    {
        builder.ToTable("tv_displays");
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.PlayerToken).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Sector).HasMaxLength(120);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.Resolution).HasMaxLength(32);
        builder.Property(x => x.PlayerToken).HasMaxLength(64).IsRequired();
        builder.Property(x => x.WeatherCity).HasMaxLength(120);
        builder.HasOne(x => x.Layout).WithMany().HasForeignKey(x => x.LayoutId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class TvLayoutConfiguration : IEntityTypeConfiguration<TvLayout>
{
    public void Configure(EntityTypeBuilder<TvLayout> builder)
    {
        builder.ToTable("tv_layouts");
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.ZonesJson).HasColumnType("text").IsRequired();
    }
}

public class TvMediaConfiguration : IEntityTypeConfiguration<TvMedia>
{
    public void Configure(EntityTypeBuilder<TvMedia> builder)
    {
        builder.ToTable("tv_midias");
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(120);
        builder.Property(x => x.Sector).HasMaxLength(120);
    }
}

public class TvPlaylistConfiguration : IEntityTypeConfiguration<TvPlaylist>
{
    public void Configure(EntityTypeBuilder<TvPlaylist> builder)
    {
        builder.ToTable("tv_playlists");
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.HasOne(x => x.Display).WithMany().HasForeignKey(x => x.DisplayId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TvPlaylistItemConfiguration : IEntityTypeConfiguration<TvPlaylistItem>
{
    public void Configure(EntityTypeBuilder<TvPlaylistItem> builder)
    {
        builder.ToTable("tv_playlist_items");
        builder.HasIndex(x => new { x.PlaylistId, x.MediaId }).IsUnique();
        builder.HasOne(x => x.Playlist).WithMany(p => p.Items).HasForeignKey(x => x.PlaylistId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Media).WithMany().HasForeignKey(x => x.MediaId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TvNewsItemConfiguration : IEntityTypeConfiguration<TvNewsItem>
{
    public void Configure(EntityTypeBuilder<TvNewsItem> builder)
    {
        builder.ToTable("tv_noticias");
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(1000);
        builder.Property(x => x.ImageUrl).HasMaxLength(500);
        builder.Property(x => x.Sector).HasMaxLength(120);
    }
}

public class TvAnnouncementConfiguration : IEntityTypeConfiguration<TvAnnouncement>
{
    public void Configure(EntityTypeBuilder<TvAnnouncement> builder)
    {
        builder.ToTable("tv_avisos");
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Body).HasColumnType("text").IsRequired();
        builder.Property(x => x.Sector).HasMaxLength(120);
    }
}

public class TvQueueCallConfiguration : IEntityTypeConfiguration<TvQueueCall>
{
    public void Configure(EntityTypeBuilder<TvQueueCall> builder)
    {
        builder.ToTable("tv_chamadas");
        builder.Property(x => x.TicketNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.PatientName).HasMaxLength(200);
        builder.Property(x => x.Destination).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Sector).HasMaxLength(120);
        builder.HasIndex(x => x.CalledAt);
        builder.HasOne(x => x.Display).WithMany().HasForeignKey(x => x.DisplayId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class TvWeatherSnapshotConfiguration : IEntityTypeConfiguration<TvWeatherSnapshot>
{
    public void Configure(EntityTypeBuilder<TvWeatherSnapshot> builder)
    {
        builder.ToTable("tv_clima");
        builder.HasIndex(x => x.City).IsUnique();
        builder.Property(x => x.City).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Condition).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(16);
    }
}

public class TvDisplayLogConfiguration : IEntityTypeConfiguration<TvDisplayLog>
{
    public void Configure(EntityTypeBuilder<TvDisplayLog> builder)
    {
        builder.ToTable("tv_logs");
        builder.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(1000);
        builder.HasIndex(x => new { x.DisplayId, x.OccurredAt });
        builder.HasOne(x => x.Display).WithMany().HasForeignKey(x => x.DisplayId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TvCampaignConfiguration : IEntityTypeConfiguration<TvCampaign>
{
    public void Configure(EntityTypeBuilder<TvCampaign> builder)
    {
        builder.ToTable("tv_campanhas");
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Sector).HasMaxLength(120);
        builder.Property(x => x.DaysOfWeek).HasMaxLength(32);
    }
}

public class TvCampaignMediaConfiguration : IEntityTypeConfiguration<TvCampaignMedia>
{
    public void Configure(EntityTypeBuilder<TvCampaignMedia> builder)
    {
        builder.ToTable("tv_campanha_midias");
        builder.HasIndex(x => new { x.CampaignId, x.MediaId }).IsUnique();
        builder.HasOne(x => x.Campaign).WithMany(c => c.MediaLinks).HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Media).WithMany().HasForeignKey(x => x.MediaId).OnDelete(DeleteBehavior.Cascade);
    }
}
