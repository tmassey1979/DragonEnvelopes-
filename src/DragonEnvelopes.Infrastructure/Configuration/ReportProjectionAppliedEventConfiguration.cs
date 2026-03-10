using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class ReportProjectionAppliedEventConfiguration : IEntityTypeConfiguration<ReportProjectionAppliedEvent>
{
    public void Configure(EntityTypeBuilder<ReportProjectionAppliedEvent> builder)
    {
        builder.ToTable("report_projection_applied_events");

        builder.HasKey(x => x.OutboxMessageId);

        builder.Property(x => x.OutboxMessageId)
            .ValueGeneratedNever();

        builder.Property(x => x.EventId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.FamilyId)
            .IsRequired(false);

        builder.Property(x => x.RoutingKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.SourceService)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.EventOccurredAtUtc)
            .IsRequired();

        builder.Property(x => x.AppliedAtUtc)
            .IsRequired();

        builder.Property(x => x.ProcessingStatus)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.HasIndex(x => new { x.FamilyId, x.AppliedAtUtc });
        builder.HasIndex(x => new { x.ProcessingStatus, x.AppliedAtUtc });
        builder.HasIndex(x => x.EventId);
        builder.HasIndex(x => new { x.SourceService, x.RoutingKey, x.AppliedAtUtc });

        builder.HasOne<IntegrationOutboxMessage>()
            .WithMany()
            .HasForeignKey(x => x.OutboxMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
