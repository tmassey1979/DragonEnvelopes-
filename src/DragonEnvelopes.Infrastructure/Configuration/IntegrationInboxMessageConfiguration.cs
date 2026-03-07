using DragonEnvelopes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DragonEnvelopes.Infrastructure.Configuration;

public sealed class IntegrationInboxMessageConfiguration : IEntityTypeConfiguration<IntegrationInboxMessage>
{
    public void Configure(EntityTypeBuilder<IntegrationInboxMessage> builder)
    {
        builder.ToTable("integration_inbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ConsumerName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.SourceService)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.EventId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.EventName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.RoutingKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.SchemaVersion)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.FamilyId)
            .IsRequired(false);

        builder.Property(x => x.PayloadJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.ReceivedAtUtc)
            .IsRequired();

        builder.Property(x => x.AttemptCount)
            .IsRequired();

        builder.Property(x => x.LastAttemptAtUtc)
            .IsRequired(false);

        builder.Property(x => x.ProcessedAtUtc)
            .IsRequired(false);

        builder.Property(x => x.DeadLetteredAtUtc)
            .IsRequired(false);

        builder.Property(x => x.LastError)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.ConsumerName,
            x.DeadLetteredAtUtc,
            x.ReceivedAtUtc
        });

        builder.HasIndex(x => new
        {
            x.SourceService,
            x.EventId
        });

        builder.HasOne<Family>()
            .WithMany()
            .HasForeignKey(x => x.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
