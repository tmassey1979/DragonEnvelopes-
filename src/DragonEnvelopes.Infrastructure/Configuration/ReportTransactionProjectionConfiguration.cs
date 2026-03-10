using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class ReportTransactionProjectionConfiguration : IEntityTypeConfiguration<ReportTransactionProjection>
{
    public void Configure(EntityTypeBuilder<ReportTransactionProjection> builder)
    {
        builder.ToTable("report_transaction_projections");

        builder.HasKey(x => x.TransactionId);

        builder.Property(x => x.TransactionId)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.AccountId)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasMaxLength(120)
            .IsRequired(false);

        builder.Property(x => x.OccurredAt)
            .IsRequired();

        builder.Property(x => x.TransferId)
            .IsRequired(false);

        builder.Property(x => x.IsDeleted)
            .IsRequired();

        builder.Property(x => x.LastEventId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.LastEventOccurredAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.FamilyId, x.OccurredAt });
        builder.HasIndex(x => new { x.FamilyId, x.Category, x.OccurredAt });
        builder.HasIndex(x => new { x.FamilyId, x.IsDeleted, x.OccurredAt });
        builder.HasIndex(x => x.LastEventOccurredAtUtc);

        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
