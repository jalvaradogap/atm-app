using Atm.Infrastructure.Persistence;
using FluentAssertions;

namespace Atm.Infrastructure.Tests;

public class DatabaseInitializerTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Seeds_CheckingAndSavings_WithTheirOpeningBalances()
    {
        using var db = _database.CreateContext();

        await DatabaseInitializer.InitializeAsync(db, CancellationToken.None);

        var accounts = await new AccountRepository(db).ListAsync(CancellationToken.None);
        accounts.Select(a => (a.Name, a.Balance)).Should().Equal(("Checking", 1_000m), ("Savings", 5_000m));
    }

    [Fact]
    public async Task RunningAgain_DoesNotSeedDuplicateAccounts()
    {
        using (var first = _database.CreateContext())
        {
            await DatabaseInitializer.InitializeAsync(first, CancellationToken.None);
        }

        using var second = _database.CreateContext();
        await DatabaseInitializer.InitializeAsync(second, CancellationToken.None);

        var accounts = await new AccountRepository(second).ListAsync(CancellationToken.None);
        accounts.Should().HaveCount(2);
    }
}
