using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class SpendAnomalyEventConfiguration : IEntityTypeConfiguration<SpendAnomalyEvent>
{
    public void Configure(EntityTypeBuilder<SpendAnomalyEvent> builder)
    {
        builder.ToTable("spend_anomaly_events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.TransactionId)
            .IsRequired();

        builder.Property(x => x.AccountId)
            .IsRequired();

        builder.Property(x => x.Merchant)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.BaselineAverageAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.BaselineStandardDeviation)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.BaselineSampleSize)
            .IsRequired();

        builder.Property(x => x.DeviationRatio)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.SeverityScore)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.DetectedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.FamilyId, x.DetectedAtUtc });
        builder.HasIndex(x => x.TransactionId)
            .IsUnique();

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
