using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.Month)
            .HasConversion(
                value => value.ToString(),
                value => BudgetMonth.Parse(value))
            .HasMaxLength(7)
            .IsRequired();

        builder.Property(x => x.TotalIncome)
            .HasConversion(
                value => value.Amount,
                value => Money.FromDecimal(value))
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Ignore(x => x.Allocations);
        builder.Ignore(x => x.AllocatedAmount);
        builder.Ignore(x => x.RemainingAmount);

        builder.HasIndex(x => new { x.FamilyId, x.Month })
            .IsUnique();

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

