using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class OnboardingProfileConfiguration : IEntityTypeConfiguration<OnboardingProfile>
{
    public void Configure(EntityTypeBuilder<OnboardingProfile> builder)
    {
        builder.ToTable("onboarding_profiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.AccountsCompleted)
            .IsRequired();

        builder.Property(x => x.EnvelopesCompleted)
            .IsRequired();

        builder.Property(x => x.BudgetCompleted)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.Property(x => x.CompletedAtUtc)
            .IsRequired(false);

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.FamilyId).IsUnique();
    }
}
