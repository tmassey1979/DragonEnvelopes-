using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class FamilyFinancialProfileConfiguration : IEntityTypeConfiguration<FamilyFinancialProfile>
{
    public void Configure(EntityTypeBuilder<FamilyFinancialProfile> builder)
    {
        builder.ToTable("family_financial_profiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.PlaidItemId)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(x => x.PlaidAccessToken)
            .HasMaxLength(512)
            .IsRequired(false);

        builder.Property(x => x.StripeCustomerId)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(x => x.StripeDefaultPaymentMethodId)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.FamilyId)
            .IsUnique();
    }
}
