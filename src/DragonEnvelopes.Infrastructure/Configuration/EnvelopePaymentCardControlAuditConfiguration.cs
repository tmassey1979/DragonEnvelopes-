using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class EnvelopePaymentCardControlAuditConfiguration : IEntityTypeConfiguration<EnvelopePaymentCardControlAudit>
{
    public void Configure(EntityTypeBuilder<EnvelopePaymentCardControlAudit> builder)
    {
        builder.ToTable("envelope_payment_card_control_audits");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.EnvelopeId)
            .IsRequired();

        builder.Property(x => x.CardId)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.PreviousStateJson)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.NewStateJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ChangedBy)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.ChangedAtUtc)
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

        builder.HasIndex(x => new { x.CardId, x.ChangedAtUtc });
    }
}
