using Atm.Application.Accounts;
using Atm.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Atm.Infrastructure.Persistence;

public sealed class AccountRepository(AtmDbContext db) : IAccountRepository
{
    public async Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken) =>
        await db.Accounts
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

    public async Task<Account?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetTransactionsPageAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var entries = db.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId);

        var totalCount = await entries.CountAsync(cancellationToken);

        // Id breaks ties between entries with the same timestamp, so pages never overlap or skip.
        var items = await entries
            .OrderByDescending(t => t.OccurredAt)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await db.SaveChangesAsync(cancellationToken);
}
