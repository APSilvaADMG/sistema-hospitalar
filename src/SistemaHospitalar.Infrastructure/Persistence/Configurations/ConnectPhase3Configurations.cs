using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaHospitalar.Domain.Entities;
using SistemaHospitalar.Domain.Enums;

namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;

public class ConnectCalendarEventConfiguration : IEntityTypeConfiguration<ConnectCalendarEvent>
{
    public void Configure(EntityTypeBuilder<ConnectCalendarEvent> builder)
    {
        builder.ToTable("connect_calendar_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Titulo).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Descricao).HasColumnType("text");
        builder.Property(e => e.Local).HasMaxLength(300);
        builder.Property(e => e.Color).HasMaxLength(20);
        builder.Property(e => e.RecurrenceRule).HasDefaultValue(ConnectCalendarRecurrenceRule.None);
        builder.HasOne(e => e.Organizador).WithMany().HasForeignKey(e => e.OrganizadorId);
        builder.HasOne(e => e.Setor).WithMany().HasForeignKey(e => e.SetorId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(e => new { e.Inicio, e.Fim });
        builder.HasIndex(e => new { e.OrganizadorId, e.Inicio });
        builder.HasIndex(e => new { e.SetorId, e.Inicio });
    }
}

public class ConnectCalendarParticipantConfiguration : IEntityTypeConfiguration<ConnectCalendarParticipant>
{
    public void Configure(EntityTypeBuilder<ConnectCalendarParticipant> builder)
    {
        builder.ToTable("connect_calendar_participants");
        builder.HasKey(p => p.Id);
        builder.HasOne(p => p.Event).WithMany(e => e.Participants).HasForeignKey(p => p.EventId);
        builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId);
        builder.HasIndex(p => new { p.EventId, p.UserId }).IsUnique();
        builder.HasIndex(p => new { p.UserId, p.EventId });
    }
}

public class ConnectContextLinkConfiguration : IEntityTypeConfiguration<ConnectContextLink>
{
    public void Configure(EntityTypeBuilder<ConnectContextLink> builder)
    {
        builder.ToTable("connect_context_links");
        builder.HasKey(l => l.Id);
        builder.HasOne(l => l.Message).WithMany().HasForeignKey(l => l.MessageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.ChatRoom).WithMany().HasForeignKey(l => l.ChatRoomId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(l => l.Ticket).WithMany().HasForeignKey(l => l.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(l => new { l.ContextType, l.ContextId, l.CreatedAt });
        builder.HasIndex(l => l.MessageId);
    }
}
