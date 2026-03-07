using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class EnvelopeRolloverRunConfiguration : IEntityTypeConfiguration<EnvelopeRolloverRun>
{
    public void Configure(EntityTypeBuilder<EnvelopeRolloverRun> builder)
    {
        builder.ToTable("envelope_rollover_runs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.Month)
            .HasMaxLength(7)
            .IsRequired();

        builder.Property(x => x.AppliedAtUtc)
            .IsRequired();

        builder.Property(x => x.AppliedByUserId)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(x => x.EnvelopeCount)
            .IsRequired();

        builder.Property(x => x.TotalRolloverBalance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.ResultJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(x => new { x.FamilyId, x.Month })
            .IsUnique();

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
