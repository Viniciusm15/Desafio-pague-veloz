using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PagueVeloz.Infrastructure.Messaging.Outbox;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.OccurredOn)
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .IsRequired(false);

        builder.Property(x => x.Attempts)
            .HasDefaultValue(0);

        builder.Property(x => x.NextAttemptAt)
            .IsRequired(false);

        builder.HasIndex(x => new { x.ProcessedAt, x.NextAttemptAt })
            .HasDatabaseName("IX_OutboxMessages_ProcessedAt_NextAttemptAt");
    }
}
