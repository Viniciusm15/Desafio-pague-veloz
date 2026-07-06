using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.CustomerId)
            .IsRequired();

        builder.Property(a => a.AvailableBalance)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(a => a.CustomerId);
    }
}
