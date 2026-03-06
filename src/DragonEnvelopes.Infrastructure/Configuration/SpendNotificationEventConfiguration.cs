using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class SpendNotificationEventConfiguration : IEntityTypeConfiguration<SpendNotificationEvent>
{
    public void Configure(EntityTypeBuilder<SpendNotificationEvent> builder)
    {
        builder.ToTable("spend_notification_events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.EnvelopeId)
            .IsRequired();

        builder.Property(x => x.CardId)
            .IsRequired();

        builder.Property(x => x.WebhookEventId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Channel)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Merchant)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.RemainingBalance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.AttemptCount)
            .IsRequired();

        builder.Property(x => x.LastAttemptAtUtc)
            .IsRequired(false);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.SentAtUtc)
            .IsRequired(false);

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Envelope>()
            .WithMany()
            .HasForeignKey(x => x.EnvelopeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<EnvelopePaymentCard>()
            .WithMany()
            .HasForeignKey(x => x.CardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.Status, x.AttemptCount, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.FamilyId, x.UserId, x.CreatedAtUtc });
        builder.HasIndex(x => x.WebhookEventId);
    }
}
