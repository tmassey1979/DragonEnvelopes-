using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class EnvelopePaymentCardConfiguration : IEntityTypeConfiguration<EnvelopePaymentCard>
{
    public void Configure(EntityTypeBuilder<EnvelopePaymentCard> builder)
    {
        builder.ToTable("envelope_payment_cards");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.EnvelopeId)
            .IsRequired();

        builder.Property(x => x.EnvelopeFinancialAccountId)
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.ProviderCardId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Brand)
            .HasMaxLength(64)
            .IsRequired(false);

        builder.Property(x => x.Last4)
            .HasMaxLength(8)
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

        builder.HasOne<EnvelopeFinancialAccount>()
            .WithMany()
            .HasForeignKey(x => x.EnvelopeFinancialAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.FamilyId, x.EnvelopeId });
        builder.HasIndex(x => new { x.Provider, x.ProviderCardId }).IsUnique();
    }
}
