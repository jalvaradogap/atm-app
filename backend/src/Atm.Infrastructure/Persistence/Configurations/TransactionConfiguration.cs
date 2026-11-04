using Atm.Domain.Accounts;
using Atm.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atm.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(t => t.Id);
        // The domain assigns ids. Without this, EF's Guid default would treat a new entry added through
        // its account as an existing row, and save it as an UPDATE that matches nothing.
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnName("AmountCents")
            .HasConversion(CentsConverter.Instance);

        builder.Property(t => t.BalanceAfter)
            .HasColumnName("BalanceAfterCents")
            .HasConversion(CentsConverter.Instance);

        // SQLite returns DateTime without a Kind. Marking it UTC keeps JSON output unambiguous.
        builder.Property(t => t.OccurredAt)
            .HasConversion(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        // Mapped explicitly. Without this the Transaction constructor could not bind to it on load.
        builder.Property(t => t.TransferId);
        builder.Property(t => t.Description).HasMaxLength(200);

        // Matches the paged history query: one account's entries, newest first.
        builder.HasIndex(t => new { t.AccountId, t.OccurredAt, t.Id });
    }
}
