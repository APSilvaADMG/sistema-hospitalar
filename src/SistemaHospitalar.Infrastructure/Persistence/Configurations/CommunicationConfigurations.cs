using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class InternalMessageConfiguration : IEntityTypeConfiguration<InternalMessage>
{
    public void Configure(EntityTypeBuilder<InternalMessage> builder)
    {
        builder.ToTable("internal_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Subject).HasMaxLength(300).IsRequired();
        builder.Property(m => m.Content).HasColumnType("text").IsRequired();
        builder.HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId);
        builder.HasOne(m => m.Patient).WithMany().HasForeignKey(m => m.PatientId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.TissGuide).WithMany().HasForeignKey(m => m.TissGuideId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.SusGuide).WithMany().HasForeignKey(m => m.SusGuideId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.Appointment).WithMany().HasForeignKey(m => m.AppointmentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(m => new { m.SenderId, m.Status, m.CreatedAt });
        builder.HasIndex(m => new { m.PatientId, m.CreatedAt });
        builder.HasIndex(m => new { m.TissGuideId, m.CreatedAt });
        builder.HasIndex(m => new { m.SusGuideId, m.CreatedAt });
    }
}

public class InternalMessageRecipientConfiguration : IEntityTypeConfiguration<InternalMessageRecipient>
{
    public void Configure(EntityTypeBuilder<InternalMessageRecipient> builder)
    {
        builder.ToTable("internal_message_recipients");
        builder.HasKey(r => r.Id);
        builder.HasOne(r => r.Message).WithMany(m => m.Recipients).HasForeignKey(r => r.MessageId);
        builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId);
        builder.HasIndex(r => new { r.UserId, r.Folder, r.IsRead });
        builder.HasIndex(r => new { r.MessageId, r.UserId }).IsUnique();
    }
}

public class InternalMessageAttachmentConfiguration : IEntityTypeConfiguration<InternalMessageAttachment>
{
    public void Configure(EntityTypeBuilder<InternalMessageAttachment> builder)
    {
        builder.ToTable("internal_message_attachments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.FileName).HasMaxLength(260).IsRequired();
        builder.Property(a => a.StoragePath).HasMaxLength(500);
        builder.Property(a => a.ContentBase64).HasColumnType("text");
        builder.Property(a => a.MimeType).HasMaxLength(120).IsRequired();
        builder.HasOne(a => a.Message).WithMany(m => m.Attachments).HasForeignKey(a => a.MessageId);
    }
}

public class ChatRoomConfiguration : IEntityTypeConfiguration<ChatRoom>
{
    public void Configure(EntityTypeBuilder<ChatRoom> builder)
    {
        builder.ToTable("chat_rooms");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.HasOne(r => r.Sector).WithMany().HasForeignKey(r => r.SectorId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(r => r.CreatedByUser).WithMany().HasForeignKey(r => r.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ChatParticipantConfiguration : IEntityTypeConfiguration<ChatParticipant>
{
    public void Configure(EntityTypeBuilder<ChatParticipant> builder)
    {
        builder.ToTable("chat_participants");
        builder.HasKey(p => p.Id);
        builder.HasOne(p => p.Room).WithMany(r => r.Participants).HasForeignKey(p => p.RoomId);
        builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId);
        builder.HasIndex(p => new { p.RoomId, p.UserId }).IsUnique();
    }
}

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Content).HasColumnType("text").IsRequired();
        builder.HasOne(m => m.Room).WithMany(r => r.Messages).HasForeignKey(m => m.RoomId);
        builder.HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId);
        builder.HasIndex(m => new { m.RoomId, m.CreatedAt });
    }
}

public class ConnectNotificationConfiguration : IEntityTypeConfiguration<ConnectNotification>
{
    public void Configure(EntityTypeBuilder<ConnectNotification> builder)
    {
        builder.ToTable("connect_notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.RelatedEntityType).HasMaxLength(80);
        builder.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId);
        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
    }
}

public class BulletinPostConfiguration : IEntityTypeConfiguration<BulletinPost>
{
    public void Configure(EntityTypeBuilder<BulletinPost> builder)
    {
        builder.ToTable("bulletin_posts");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Content).HasColumnType("text").IsRequired();
        builder.HasOne(p => p.Author).WithMany().HasForeignKey(p => p.AuthorId);
        builder.HasIndex(p => new { p.IsPinned, p.PublishedAt });
    }
}

public class BulletinViewConfiguration : IEntityTypeConfiguration<BulletinView>
{
    public void Configure(EntityTypeBuilder<BulletinView> builder)
    {
        builder.ToTable("bulletin_views");
        builder.HasKey(v => v.Id);
        builder.HasOne(v => v.Bulletin).WithMany(p => p.Views).HasForeignKey(v => v.BulletinId);
        builder.HasOne(v => v.User).WithMany().HasForeignKey(v => v.UserId);
        builder.HasIndex(v => new { v.BulletinId, v.UserId }).IsUnique();
    }
}

public class CommunicationAuditLogConfiguration : IEntityTypeConfiguration<CommunicationAuditLog>
{
    public void Configure(EntityTypeBuilder<CommunicationAuditLog> builder)
    {
        builder.ToTable("communication_audit_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Action).HasMaxLength(80).IsRequired();
        builder.Property(l => l.EntityType).HasMaxLength(80).IsRequired();
        builder.Property(l => l.Details).HasMaxLength(2000);
        builder.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(l => l.CreatedAt);
    }
}
