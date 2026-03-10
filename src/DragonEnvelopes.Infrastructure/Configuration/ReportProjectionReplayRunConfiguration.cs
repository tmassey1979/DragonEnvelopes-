using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class ReportProjectionReplayRunConfiguration : IEntityTypeConfiguration<ReportProjectionReplayRun>
{
    public void Configure(EntityTypeBuilder<ReportProjectionReplayRun> builder)
    {
        builder.ToTable("report_projection_replay_runs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired(false);

        builder.Property(x => x.ProjectionSet)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.FromOccurredAtUtc)
            .IsRequired(false);

        builder.Property(x => x.ToOccurredAtUtc)
            .IsRequired(false);

        builder.Property(x => x.IsDryRun)
            .IsRequired();

        builder.Property(x => x.ResetState)
            .IsRequired();

        builder.Property(x => x.BatchSize)
            .IsRequired();

        builder.Property(x => x.MaxEvents)
            .IsRequired();

        builder.Property(x => x.ThrottleMilliseconds)
            .IsRequired();

        builder.Property(x => x.TargetedEventCount)
            .IsRequired();

        builder.Property(x => x.ProcessedEventCount)
            .IsRequired();

        builder.Property(x => x.AppliedCount)
            .IsRequired();

        builder.Property(x => x.FailedCount)
            .IsRequired();

        builder.Property(x => x.BatchesProcessed)
            .IsRequired();

        builder.Property(x => x.WasCappedByMaxEvents)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.RequestedByUserId)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(x => x.StartedAtUtc)
            .IsRequired();

        builder.Property(x => x.CompletedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.FamilyId, x.StartedAtUtc });
        builder.HasIndex(x => new { x.Status, x.StartedAtUtc });
        builder.HasIndex(x => new { x.ProjectionSet, x.StartedAtUtc });

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
