using Atm.Domain.Accounts;
using Atm.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atm.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Name).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Number).HasMaxLength(20).IsRequired();

        builder.Property(a => a.Balance)
            .HasColumnName("BalanceCents")
            .HasConversion(CentsConverter.Instance);

        // Checked on every UPDATE: "WHERE Version = <value last read>". A stale write matches no row.
        builder.Property(a => a.Version).IsConcurrencyToken();

        // The ledger is owned by the account, so it is written and deleted with it.
        // The backing field is used because Account exposes the ledger as a read-only list.
        builder.HasMany(a => a.Transactions)
            .WithOne()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.Transactions)
            .HasField("_transactions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
