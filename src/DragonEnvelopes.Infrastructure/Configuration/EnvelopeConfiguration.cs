using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class EnvelopeConfiguration : IEntityTypeConfiguration<Envelope>
{
    public void Configure(EntityTypeBuilder<Envelope> builder)
    {
        builder.ToTable("envelopes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.MonthlyBudget)
            .HasConversion(
                value => value.Amount,
                value => Money.FromDecimal(value))
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.CurrentBalance)
            .HasConversion(
                value => value.Amount,
                value => Money.FromDecimal(value))
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.RolloverMode)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.RolloverCap)
            .HasConversion(
                value => value.HasValue ? value.Value.Amount : (decimal?)null,
                value => value.HasValue ? Money.FromDecimal(value.Value) : null)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(x => x.LastActivityAt)
            .IsRequired(false);

        builder.Property(x => x.IsArchived)
            .IsRequired();

        builder.HasIndex(x => new { x.FamilyId, x.Name })
            .IsUnique();

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
