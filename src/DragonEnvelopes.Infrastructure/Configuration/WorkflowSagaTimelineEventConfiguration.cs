using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class WorkflowSagaTimelineEventConfiguration : IEntityTypeConfiguration<WorkflowSagaTimelineEvent>
{
    public void Configure(EntityTypeBuilder<WorkflowSagaTimelineEvent> builder)
    {
        builder.ToTable("workflow_saga_timeline_events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.SagaId)
            .IsRequired();

        builder.Property(x => x.FamilyId)
            .IsRequired(false);

        builder.Property(x => x.WorkflowType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Step)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.SagaId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.FamilyId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.WorkflowType, x.OccurredAtUtc });

        builder.HasOne<WorkflowSaga>()
            .WithMany()
            .HasForeignKey(x => x.SagaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
