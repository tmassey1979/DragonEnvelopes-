using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.AccountId)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasConversion(
                value => value.Amount,
                value => Money.FromDecimal(value))
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Merchant)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.EnvelopeId)
            .IsRequired(false);

        builder.Property(x => x.DeletedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.DeletedByUserId)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.HasIndex(x => x.DeletedAtUtc);

        builder.Ignore(x => x.Splits);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Envelope>()
            .WithMany()
            .HasForeignKey(x => x.EnvelopeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
