using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class FamilyApprovalPolicyConfiguration : IEntityTypeConfiguration<FamilyApprovalPolicy>
{
    public void Configure(EntityTypeBuilder<FamilyApprovalPolicy> builder)
    {
        builder.ToTable("family_approval_policies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.Property(x => x.AmountThreshold)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.RolesRequiringApprovalCsv)
            .HasMaxLength(128)
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
