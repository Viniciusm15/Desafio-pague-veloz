using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Infrastructure.Persistence.Configurations;

public class AccountOperationConfiguration : IEntityTypeConfiguration<AccountOperation>
{
    public void Configure(EntityTypeBuilder<AccountOperation> builder)
    {
        builder.ToTable("AccountOperations");
        builder.HasKey(o => o.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(o => o.Id)
            .ValueGeneratedNever();

        builder.Property(o => o.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.ReferenceId)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(o => new { o.AccountId, o.ReferenceId })
            .IsUnique();

        builder.Property(o => o.OccurredAt)
            .IsRequired();
    }
}
