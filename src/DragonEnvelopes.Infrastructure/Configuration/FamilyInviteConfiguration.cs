using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class FamilyInviteConfiguration : IEntityTypeConfiguration<FamilyInvite>
{
    public void Configure(EntityTypeBuilder<FamilyInvite> builder)
    {
        builder.ToTable("family_invites");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.Role)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.AcceptedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.CancelledAtUtc)
            .IsRequired(false);

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.FamilyId, x.Status });
        builder.HasIndex(x => x.TokenHash).IsUnique();
    }
}
