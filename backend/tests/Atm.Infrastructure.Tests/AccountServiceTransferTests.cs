using Atm.Application.Accounts;
using Atm.Domain.Accounts;
using Atm.Domain.Exceptions;
using Atm.Infrastructure.Persistence;
using FluentAssertions;

namespace Atm.Infrastructure.Tests;

/// <summary>Transfers through the real service and SQLite, so atomicity is tested end to end.</summary>
public class AccountServiceTransferTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public AccountServiceTransferTests()
    {
        using var db = _database.CreateContext();
        DatabaseInitializer.InitializeAsync(db, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Transfer_UpdatesBothBalances_AndWritesTwoLegsSharingOneTransferId()
    {
        var (checkingId, savingsId) = await GetSeededIdsAsync();

        Guid transferId;
        using (var db = _database.CreateContext())
        {
            var result = await ServiceFor(db).TransferAsync(checkingId, savingsId, 250.50m, "Rent", CancellationToken.None);
            transferId = result.TransferId;
        }

        using var check = _database.CreateContext();
        var checking = await new AccountRepository(check).FindAsync(checkingId, CancellationToken.None);
        var savings = await new AccountRepository(check).FindAsync(savingsId, CancellationToken.None);
        checking!.Balance.Should().Be(749.50m);
        savings!.Balance.Should().Be(5_250.50m);

        var outLeg = await SingleLegAsync(check, checkingId, transferId);
        outLeg.Type.Should().Be(TransactionType.TransferOut);
        outLeg.Amount.Should().Be(250.50m);

        var inLeg = await SingleLegAsync(check, savingsId, transferId);
        inLeg.Type.Should().Be(TransactionType.TransferIn);
        inLeg.Amount.Should().Be(250.50m);
    }

    [Fact]
    public async Task Transfer_WithInsufficientFunds_ChangesNeitherAccount()
    {
        var (checkingId, savingsId) = await GetSeededIdsAsync();

        using (var db = _database.CreateContext())
        {
            var act = () => ServiceFor(db).TransferAsync(checkingId, savingsId, 2_000m, null, CancellationToken.None);
            await act.Should().ThrowAsync<InsufficientFundsException>();
        }

        using var check = _database.CreateContext();
        var repository = new AccountRepository(check);
        (await repository.FindAsync(checkingId, CancellationToken.None))!.Balance.Should().Be(1_000m);
        (await repository.FindAsync(savingsId, CancellationToken.None))!.Balance.Should().Be(5_000m);
    }

    [Fact]
    public async Task Transfer_ToTheSameAccount_IsRejected_AndChangesNothing()
    {
        var (checkingId, _) = await GetSeededIdsAsync();

        using (var db = _database.CreateContext())
        {
            var act = () => ServiceFor(db).TransferAsync(checkingId, checkingId, 10m, null, CancellationToken.None);
            await act.Should().ThrowAsync<SameAccountTransferException>();
        }

        using var check = _database.CreateContext();
        (await new AccountRepository(check).FindAsync(checkingId, CancellationToken.None))!
            .Balance.Should().Be(1_000m);
    }

    private static AccountService ServiceFor(AtmDbContext db) =>
        new(new AccountRepository(db));

    private async Task<(Guid CheckingId, Guid SavingsId)> GetSeededIdsAsync()
    {
        using var db = _database.CreateContext();
        var accounts = await new AccountRepository(db).ListAsync(CancellationToken.None);
        return (accounts.Single(a => a.Name == "Checking").Id, accounts.Single(a => a.Name == "Savings").Id);
    }

    private static async Task<Transaction> SingleLegAsync(AtmDbContext db, Guid accountId, Guid transferId)
    {
        var (entries, _) = await new AccountRepository(db).GetTransactionsPageAsync(
            accountId, page: 1, pageSize: 20, CancellationToken.None);

        return entries.Should().ContainSingle(t => t.TransferId == transferId).Subject;
    }
}
