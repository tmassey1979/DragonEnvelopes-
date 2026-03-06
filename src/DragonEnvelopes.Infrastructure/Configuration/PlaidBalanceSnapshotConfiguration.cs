using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class PlaidBalanceSnapshotConfiguration : IEntityTypeConfiguration<PlaidBalanceSnapshot>
{
    public void Configure(EntityTypeBuilder<PlaidBalanceSnapshot> builder)
    {
        builder.ToTable("plaid_balance_snapshots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.AccountId)
            .IsRequired();

        builder.Property(x => x.PlaidAccountId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.InternalBalanceBefore)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.ProviderBalance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.InternalBalanceAfter)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.DriftAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.RefreshedAtUtc)
            .IsRequired();

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.FamilyId, x.RefreshedAtUtc });
        builder.HasIndex(x => new { x.FamilyId, x.AccountId, x.RefreshedAtUtc });
    }
}
