using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class PlaidWebhookEventConfiguration : IEntityTypeConfiguration<PlaidWebhookEvent>
{
    public void Configure(EntityTypeBuilder<PlaidWebhookEvent> builder)
    {
        builder.ToTable("plaid_webhook_events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.WebhookType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.WebhookCode)
            .HasMaxLength(64)
            .IsRequired(false);

        builder.Property(x => x.ItemId)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(x => x.FamilyId)
            .IsRequired(false);

        builder.Property(x => x.ProcessingStatus)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.PayloadJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ReceivedAtUtc)
            .IsRequired();

        builder.Property(x => x.ProcessedAtUtc)
            .IsRequired();

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.FamilyId, x.ProcessedAtUtc });
        builder.HasIndex(x => new { x.ItemId, x.ProcessedAtUtc });
        builder.HasIndex(x => new { x.WebhookType, x.ProcessedAtUtc });
    }
}
