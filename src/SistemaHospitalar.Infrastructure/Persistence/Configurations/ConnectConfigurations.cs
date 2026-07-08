using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class ConnectConversationConfiguration : IEntityTypeConfiguration<ConnectConversation>
{
    public void Configure(EntityTypeBuilder<ConnectConversation> builder)
    {
        builder.ToTable("connect_conversations");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ContactPhone).HasMaxLength(30).IsRequired();
        builder.Property(c => c.ContactName).HasMaxLength(200);
        builder.Property(c => c.BotContextJson).HasColumnType("text");
        builder.HasIndex(c => new { c.Channel, c.ContactPhone });
        builder.Property(c => c.WhatsAppOptOut).HasDefaultValue(false);
        builder.HasOne(c => c.Patient).WithMany().HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(c => c.AssignedUser).WithMany().HasForeignKey(c => c.AssignedUserId).OnDelete(DeleteBehavior.SetNull);
        builder.Property(c => c.Queue).HasConversion<int>();
    }
}

public class ConnectMessageConfiguration : IEntityTypeConfiguration<ConnectMessage>
{
    public void Configure(EntityTypeBuilder<ConnectMessage> builder)
    {
        builder.ToTable("connect_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Body).HasColumnType("text").IsRequired();
        builder.Property(m => m.ExternalId).HasMaxLength(120);
        builder.Property(m => m.FailureReason).HasMaxLength(500);
        builder.HasIndex(m => m.ExternalId)
            .IsUnique()
            .HasFilter("\"ExternalId\" IS NOT NULL");
        builder.HasOne(m => m.Conversation).WithMany(c => c.Messages).HasForeignKey(m => m.ConversationId);
        builder.HasOne(m => m.SentByUser).WithMany().HasForeignKey(m => m.SentByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.Appointment).WithMany().HasForeignKey(m => m.AppointmentId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ConnectScheduledMessageConfiguration : IEntityTypeConfiguration<ConnectScheduledMessage>
{
    public void Configure(EntityTypeBuilder<ConnectScheduledMessage> builder)
    {
        builder.ToTable("connect_scheduled_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.PayloadJson).HasColumnType("text").IsRequired();
        builder.HasIndex(m => new { m.IsSent, m.ScheduledFor });
    }
}

public class ConnectWaitlistEntryConfiguration : IEntityTypeConfiguration<ConnectWaitlistEntry>
{
    public void Configure(EntityTypeBuilder<ConnectWaitlistEntry> builder)
    {
        builder.ToTable("connect_waitlist");
        builder.HasKey(w => w.Id);
        builder.HasOne(w => w.Patient).WithMany().HasForeignKey(w => w.PatientId);
        builder.HasOne(w => w.Specialty).WithMany().HasForeignKey(w => w.SpecialtyId);
        builder.HasOne(w => w.Professional).WithMany().HasForeignKey(w => w.ProfessionalId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ConnectCheckInConfiguration : IEntityTypeConfiguration<ConnectCheckIn>
{
    public void Configure(EntityTypeBuilder<ConnectCheckIn> builder)
    {
        builder.ToTable("connect_checkins");
        builder.HasKey(c => c.Id);
        builder.HasOne(c => c.Appointment).WithMany().HasForeignKey(c => c.AppointmentId);
        builder.HasOne(c => c.Patient).WithMany().HasForeignKey(c => c.PatientId);
    }
}

public class ConnectSatisfactionSurveyConfiguration : IEntityTypeConfiguration<ConnectSatisfactionSurvey>
{
    public void Configure(EntityTypeBuilder<ConnectSatisfactionSurvey> builder)
    {
        builder.ToTable("connect_satisfaction_surveys");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Comment).HasMaxLength(1000);
    }
}

public class ConnectKnowledgeArticleConfiguration : IEntityTypeConfiguration<ConnectKnowledgeArticle>
{
    public void Configure(EntityTypeBuilder<ConnectKnowledgeArticle> builder)
    {
        builder.ToTable("connect_knowledge_articles");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Category).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Question).HasMaxLength(300).IsRequired();
        builder.Property(a => a.Answer).HasColumnType("text").IsRequired();
        builder.Property(a => a.Keywords).HasMaxLength(500);
    }
}
