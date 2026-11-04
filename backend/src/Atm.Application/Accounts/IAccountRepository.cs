using Atm.Domain.Accounts;

namespace Atm.Application.Accounts;

/// <summary>
/// Persistence port for accounts. The Infrastructure layer implements it with EF Core.
/// Accounts returned by <see cref="FindAsync"/> are tracked, so <see cref="SaveChangesAsync"/>
/// persists every change made to them in one unit of work.
/// </summary>
public interface IAccountRepository
{
    Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Loads the account without its ledger. The ledger is paged separately by <see cref="GetTransactionsPageAsync"/>.</summary>
    Task<Account?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>One page of an account's ledger, newest first, plus the total count.</summary>
    Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetTransactionsPageAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
