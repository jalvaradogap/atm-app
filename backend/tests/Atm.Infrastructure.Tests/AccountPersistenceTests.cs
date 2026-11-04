using Atm.Domain.Accounts;
using Atm.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Atm.Infrastructure.Tests;

public class AccountPersistenceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Balance_AndLedgerAmounts_RoundTripExactly()
    {
        var accountId = await SaveNewAccountWithDepositsAsync(999.99m, 0.10m);

        using var db = _database.CreateContext();
        var repository = new AccountRepository(db);

        var account = await repository.FindAsync(accountId, CancellationToken.None);
        account!.Balance.Should().Be(1000.09m);

        var (entries, totalCount) = await repository.GetTransactionsPageAsync(
            accountId, page: 1, pageSize: 20, CancellationToken.None);

        totalCount.Should().Be(2);
        entries.Select(t => t.Amount).Should().BeEquivalentTo(new[] { 999.99m, 0.10m });
        entries.Select(t => t.BalanceAfter).Should().BeEquivalentTo(new[] { 999.99m, 1000.09m });
        entries.Should().OnlyContain(t => t.Type == TransactionType.Deposit);
        entries.Should().OnlyContain(t => t.OccurredAt.Kind == DateTimeKind.Utc);
    }

    [Fact]
    public async Task StaleSave_IsRejected_ByTheVersionToken()
    {
        var accountId = await SaveNewAccountWithDepositsAsync();

        using var first = _database.CreateContext();
        using var second = _database.CreateContext();

        // Both requests read the account at the same version.
        var firstRead = await new AccountRepository(first).FindAsync(accountId, CancellationToken.None);
        var secondRead = await new AccountRepository(second).FindAsync(accountId, CancellationToken.None);

        secondRead!.Deposit(100m);
        await second.SaveChangesAsync();

        // The first request's UPDATE is "WHERE Version = <old>", which now matches no row.
        firstRead!.Deposit(50m);
        var act = () => first.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    /// <summary>Opens an account and makes the given deposits in one save.</summary>
    private async Task<Guid> SaveNewAccountWithDepositsAsync(params decimal[] deposits)
    {
        using var db = _database.CreateContext();

        var account = Account.Open("Checking", "****1234");
        foreach (var amount in deposits)
        {
            account.Deposit(amount);
        }

        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        return account.Id;
    }
}
