using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class PlaidSyncCursorConfiguration : IEntityTypeConfiguration<PlaidSyncCursor>
{
    public void Configure(EntityTypeBuilder<PlaidSyncCursor> builder)
    {
        builder.ToTable("plaid_sync_cursors");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired();

        builder.Property(x => x.Cursor)
            .HasMaxLength(512)
            .IsRequired(false);

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
