using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class RecurringBillExecutionConfiguration : IEntityTypeConfiguration<RecurringBillExecution>
{
    public void Configure(EntityTypeBuilder<RecurringBillExecution> builder)
    {
        builder.ToTable("recurring_bill_executions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.RecurringBillId)
            .IsRequired();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.DueDate)
            .IsRequired();

        builder.Property(x => x.ExecutedAtUtc)
            .IsRequired();

        builder.Property(x => x.TransactionId)
            .IsRequired(false);

        builder.Property(x => x.Result)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.HasOne<RecurringBill>()
            .WithMany()
            .HasForeignKey(x => x.RecurringBillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.RecurringBillId, x.DueDate })
            .IsUnique();
        builder.HasIndex(x => new { x.FamilyId, x.DueDate });
    }
}
