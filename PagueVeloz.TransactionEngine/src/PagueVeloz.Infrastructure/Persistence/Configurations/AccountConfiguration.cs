using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;

namespace PagueVeloz.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.CustomerId)
            .IsRequired();

        builder.Property(a => a.AvailableBalance)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(a => a.ReservedBalance)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(a => a.CreditLimit)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(a => a.Status)
           .HasConversion<string>()
           .HasMaxLength(20)
           .HasDefaultValue(AccountStatus.Active)
           .HasSentinel(AccountStatus.Active);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(a => a.CustomerId);

        builder.HasMany(a => a.Operations)
            .WithOne()
            .HasForeignKey(o => o.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.Operations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
