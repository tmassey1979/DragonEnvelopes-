using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class EnvelopePaymentCardShipmentConfiguration : IEntityTypeConfiguration<EnvelopePaymentCardShipment>
{
    public void Configure(EntityTypeBuilder<EnvelopePaymentCardShipment> builder)
    {
        builder.ToTable("envelope_payment_card_shipments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.EnvelopeId)
            .IsRequired();

        builder.Property(x => x.CardId)
            .IsRequired();

        builder.Property(x => x.RecipientName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.AddressLine1)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.AddressLine2)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(x => x.City)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.StateOrProvince)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.PostalCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CountryCode)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Carrier)
            .HasMaxLength(64)
            .IsRequired(false);

        builder.Property(x => x.TrackingNumber)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(x => x.RequestedAtUtc)
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
