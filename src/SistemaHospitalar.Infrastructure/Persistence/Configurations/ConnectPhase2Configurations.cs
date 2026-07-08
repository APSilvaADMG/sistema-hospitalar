using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SistemaHospitalar.Domain.Entities;



namespace SistemaHospitalar.Infrastructure.Persistence.Configurations;



public class ConnectTicketConfiguration : IEntityTypeConfiguration<ConnectTicket>

{

    public void Configure(EntityTypeBuilder<ConnectTicket> builder)

    {

        builder.ToTable("connect_tickets");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Protocolo).HasMaxLength(30).IsRequired();

        builder.Property(t => t.Titulo).HasMaxLength(300).IsRequired();

        builder.Property(t => t.Descricao).HasColumnType("text").IsRequired();

        builder.HasOne(t => t.Solicitante).WithMany().HasForeignKey(t => t.SolicitanteId);

        builder.HasOne(t => t.Responsavel).WithMany().HasForeignKey(t => t.ResponsavelId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.Protocolo).IsUnique();

        builder.HasIndex(t => new { t.Status, t.Categoria, t.DueAt });

        builder.HasIndex(t => new { t.SolicitanteId, t.CreatedAt });

        builder.HasIndex(t => new { t.ResponsavelId, t.Status });

    }

}



public class ConnectTicketCommentConfiguration : IEntityTypeConfiguration<ConnectTicketComment>

{

    public void Configure(EntityTypeBuilder<ConnectTicketComment> builder)

    {

        builder.ToTable("connect_ticket_comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content).HasColumnType("text").IsRequired();

        builder.HasOne(c => c.Ticket).WithMany(t => t.Comments).HasForeignKey(c => c.TicketId);

        builder.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId);

        builder.HasIndex(c => new { c.TicketId, c.CreatedAt });

    }

}



public class ConnectTaskConfiguration : IEntityTypeConfiguration<ConnectTask>

{

    public void Configure(EntityTypeBuilder<ConnectTask> builder)

    {

        builder.ToTable("connect_tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Titulo).HasMaxLength(300).IsRequired();

        builder.Property(t => t.Descricao).HasColumnType("text").IsRequired();

        builder.HasOne(t => t.Criador).WithMany().HasForeignKey(t => t.CriadorId);

        builder.HasOne(t => t.Responsavel).WithMany().HasForeignKey(t => t.ResponsavelId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => new { t.ResponsavelId, t.Status, t.Prazo });

        builder.HasIndex(t => new { t.CriadorId, t.CreatedAt });

    }

}



public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>

{

    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)

    {

        builder.ToTable("workflow_instances");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Titulo).HasMaxLength(300).IsRequired();

        builder.Property(i => i.Descricao).HasColumnType("text").IsRequired();

        builder.Property(i => i.Referencia).HasMaxLength(200);

        builder.HasOne(i => i.Solicitante).WithMany().HasForeignKey(i => i.SolicitanteId);

        builder.HasIndex(i => new { i.Status, i.Tipo, i.CreatedAt });

        builder.HasIndex(i => new { i.SolicitanteId, i.CreatedAt });

    }

}



public class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>

{

    public void Configure(EntityTypeBuilder<WorkflowStep> builder)

    {

        builder.ToTable("workflow_steps");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Justificativa).HasMaxLength(2000);

        builder.HasOne(s => s.Instance).WithMany(i => i.Steps).HasForeignKey(s => s.InstanceId);

        builder.HasOne(s => s.Aprovador).WithMany().HasForeignKey(s => s.AprovadorId);

        builder.HasIndex(s => new { s.AprovadorId, s.Status });

        builder.HasIndex(s => new { s.InstanceId, s.Ordem }).IsUnique();

    }

}

