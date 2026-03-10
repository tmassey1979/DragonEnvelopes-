using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class ReportEnvelopeBalanceProjectionConfiguration : IEntityTypeConfiguration<ReportEnvelopeBalanceProjection>
{
    public void Configure(EntityTypeBuilder<ReportEnvelopeBalanceProjection> builder)
    {
        builder.ToTable("report_envelope_balance_projections");

        builder.HasKey(x => x.EnvelopeId);

        builder.Property(x => x.EnvelopeId)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.EnvelopeName)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.MonthlyBudget)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.CurrentBalance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.IsArchived)
            .IsRequired();

        builder.Property(x => x.LastEventId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.LastEventOccurredAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.FamilyId, x.EnvelopeName });
        builder.HasIndex(x => new { x.FamilyId, x.IsArchived });
        builder.HasIndex(x => x.LastEventOccurredAtUtc);

        builder.HasOne<Envelope>()
            .WithMany()
            .HasForeignKey(x => x.EnvelopeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
