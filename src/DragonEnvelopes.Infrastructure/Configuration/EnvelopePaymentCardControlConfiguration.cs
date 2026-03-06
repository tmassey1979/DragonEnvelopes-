using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class EnvelopePaymentCardControlConfiguration : IEntityTypeConfiguration<EnvelopePaymentCardControl>
{
    public void Configure(EntityTypeBuilder<EnvelopePaymentCardControl> builder)
    {
        builder.ToTable("envelope_payment_card_controls");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.EnvelopeId)
            .IsRequired();

        builder.Property(x => x.CardId)
            .IsRequired();

        builder.Property(x => x.DailyLimitAmount)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(x => x.AllowedMerchantCategoriesJson)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.AllowedMerchantNamesJson)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

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

        builder.HasIndex(x => x.CardId)
            .IsUnique();

        builder.HasIndex(x => new { x.FamilyId, x.EnvelopeId });
    }
}
