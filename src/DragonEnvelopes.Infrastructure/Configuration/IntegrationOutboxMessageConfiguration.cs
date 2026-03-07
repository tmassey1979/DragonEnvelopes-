using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class IntegrationOutboxMessageConfiguration : IEntityTypeConfiguration<IntegrationOutboxMessage>
{
    public void Configure(EntityTypeBuilder<IntegrationOutboxMessage> builder)
    {
        builder.ToTable("integration_outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FamilyId)
            .IsRequired(false);

        builder.Property(x => x.EventId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.RoutingKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.EventName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.SchemaVersion)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.SourceService)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CausationId)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(x => x.PayloadJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.AttemptCount)
            .IsRequired();

        builder.Property(x => x.NextAttemptAtUtc)
            .IsRequired();

        builder.Property(x => x.LastError)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(x => x.DispatchedAtUtc)
            .IsRequired(false);

        builder.HasIndex(x => x.EventId)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.DispatchedAtUtc,
            x.NextAttemptAtUtc,
            x.CreatedAtUtc
        });

        builder.HasIndex(x => new
        {
            x.FamilyId,
            x.CreatedAtUtc
        });

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
