using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class WorkflowSagaConfiguration : IEntityTypeConfiguration<WorkflowSaga>
{
    public void Configure(EntityTypeBuilder<WorkflowSaga> builder)
    {
        builder.ToTable("workflow_sagas");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired(false);

        builder.Property(x => x.WorkflowType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ReferenceId)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CurrentStep)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.CompensationAction)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.StartedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.Property(x => x.CompletedAtUtc)
            .IsRequired(false);

        builder.HasIndex(x => new { x.FamilyId, x.WorkflowType, x.UpdatedAtUtc });
        builder.HasIndex(x => new { x.WorkflowType, x.CorrelationId })
            .IsUnique();
        builder.HasIndex(x => new { x.Status, x.UpdatedAtUtc });

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
