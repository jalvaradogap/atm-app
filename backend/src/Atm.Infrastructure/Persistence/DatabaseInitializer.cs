using Atm.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Atm.Infrastructure.Persistence;

/// <summary>
/// Creates the schema and seeds the two demo accounts. Safe to run on every startup: seeding
/// only happens when there are no accounts yet.
/// </summary>
public static class DatabaseInitializer
{
    public const string CheckingNumber = "****1234";
    public const string SavingsNumber = "****5678";

    public static async Task InitializeAsync(AtmDbContext db, CancellationToken cancellationToken)
    {
        // EnsureCreated instead of migrations: one fixed schema, decision #4 in PLAN.md.
        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (await db.Accounts.AnyAsync(cancellationToken))
        {
            return;
        }

        // Opening balances go through Deposit, so they appear in the ledger like any other money.
        var checking = Account.Open("Checking", CheckingNumber);
        checking.Deposit(1_000m, "Opening deposit");

        var savings = Account.Open("Savings", SavingsNumber);
        savings.Deposit(5_000m, "Opening deposit");

        db.Accounts.AddRange(checking, savings);
        await db.SaveChangesAsync(cancellationToken);
    }
}
