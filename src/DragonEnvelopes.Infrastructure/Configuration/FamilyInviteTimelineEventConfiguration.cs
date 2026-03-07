using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class FamilyInviteTimelineEventConfiguration : IEntityTypeConfiguration<FamilyInviteTimelineEvent>
{
    public void Configure(EntityTypeBuilder<FamilyInviteTimelineEvent> builder)
    {
        builder.ToTable("family_invite_timeline_events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.InviteId)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.ActorUserId)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<FamilyInvite>()
            .WithMany()
            .HasForeignKey(x => x.InviteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.FamilyId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.InviteId, x.OccurredAtUtc });
    }
}
