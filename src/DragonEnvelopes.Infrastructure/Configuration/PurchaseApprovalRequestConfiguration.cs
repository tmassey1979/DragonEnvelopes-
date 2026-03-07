using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class PurchaseApprovalRequestConfiguration : IEntityTypeConfiguration<PurchaseApprovalRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseApprovalRequest> builder)
    {
        builder.ToTable("purchase_approval_requests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.AccountId)
            .IsRequired();

        builder.Property(x => x.RequestedByUserId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.RequestedByRole)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(x => x.Amount)
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

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(x => x.RequestNotes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.ResolutionNotes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.ResolvedByUserId)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(x => x.ResolvedByRole)
            .HasMaxLength(24)
            .IsRequired(false);

        builder.Property(x => x.ResolvedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.ApprovedTransactionId)
            .IsRequired(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Envelope>()
            .WithMany()
            .HasForeignKey(x => x.EnvelopeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(x => x.ApprovedTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.FamilyId, x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => x.AccountId);
    }
}
