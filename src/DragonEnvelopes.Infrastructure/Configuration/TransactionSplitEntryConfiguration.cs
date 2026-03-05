using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class TransactionSplitEntryConfiguration : IEntityTypeConfiguration<TransactionSplitEntry>
{
    public void Configure(EntityTypeBuilder<TransactionSplitEntry> builder)
    {
        builder.ToTable("transaction_splits");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.TransactionId)
            .IsRequired();

        builder.Property(x => x.EnvelopeId)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasConversion(
                value => value.Amount,
                value => Money.FromDecimal(value))
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Envelope>()
            .WithMany()
            .HasForeignKey(x => x.EnvelopeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TransactionId);
    }
}
