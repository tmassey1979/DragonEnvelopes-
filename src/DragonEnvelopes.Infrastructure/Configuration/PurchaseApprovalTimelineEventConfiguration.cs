using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class PurchaseApprovalTimelineEventConfiguration : IEntityTypeConfiguration<PurchaseApprovalTimelineEvent>
{
    public void Configure(EntityTypeBuilder<PurchaseApprovalTimelineEvent> builder)
    {
        builder.ToTable("purchase_approval_timeline_events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.ApprovalRequestId)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(x => x.ActorUserId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.ActorRole)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<PurchaseApprovalRequest>()
            .WithMany()
            .HasForeignKey(x => x.ApprovalRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.FamilyId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.ApprovalRequestId, x.OccurredAtUtc });
    }
}
