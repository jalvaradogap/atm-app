using Atm.Application.Dtos;
using Atm.Domain.Accounts;
using Atm.Domain.Exceptions;

namespace Atm.Application.Accounts;

/// <summary>
/// Use cases for the ATM. The service loads accounts, calls the domain methods that enforce the
/// rules, and saves once. It holds no business rules of its own.
/// </summary>
public sealed class AccountService(IAccountRepository accounts)
{
    public const int MaxPageSize = 100;

    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(CancellationToken cancellationToken)
    {
        var list = await accounts.ListAsync(cancellationToken);
        return list.Select(DtoMapping.ToDto).ToList();
    }

    public async Task<AccountDto> GetAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await LoadAsync(accountId, cancellationToken);
        return DtoMapping.ToDto(account);
    }

    public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Invalid paging is rejected at the API boundary. These guards catch programmer errors.
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, MaxPageSize);

        // Checked first, so an unknown account is a 404 rather than an empty history.
        await LoadAsync(accountId, cancellationToken);

        var (items, totalCount) = await accounts.GetTransactionsPageAsync(
            accountId, page, pageSize, cancellationToken);

        return new PagedResult<TransactionDto>(
            items.Select(DtoMapping.ToDto).ToList(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<AccountDto> DepositAsync(
        Guid accountId,
        decimal amount,
        string? description,
        CancellationToken cancellationToken)
    {
        var account = await LoadAsync(accountId, cancellationToken);
        account.Deposit(amount, description);
        await accounts.SaveChangesAsync(cancellationToken);

        return DtoMapping.ToDto(account);
    }

    public async Task<AccountDto> WithdrawAsync(
        Guid accountId,
        decimal amount,
        string? description,
        CancellationToken cancellationToken)
    {
        var account = await LoadAsync(accountId, cancellationToken);
        account.Withdraw(amount, description);
        await accounts.SaveChangesAsync(cancellationToken);

        return DtoMapping.ToDto(account);
    }

    /// <summary>
    /// Moves money between two accounts. Both legs are changed in memory and then saved by one
    /// <c>SaveChangesAsync</c> call, so the database writes them in one transaction. If either
    /// leg fails a domain rule, nothing is saved.
    /// </summary>
    public async Task<TransferResultDto> TransferAsync(
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        string? description,
        CancellationToken cancellationToken)
    {
        var source = await LoadAsync(fromAccountId, cancellationToken);
        var destination = await LoadAsync(toAccountId, cancellationToken);

        // The same-account rule lives in Account.TransferOut, so it is not repeated here.
        var transferId = Guid.NewGuid();
        source.TransferOut(destination.Id, transferId, amount, description);
        destination.TransferIn(source.Id, transferId, amount, description);

        await accounts.SaveChangesAsync(cancellationToken);

        return new TransferResultDto(
            transferId,
            DtoMapping.ToDto(source),
            DtoMapping.ToDto(destination));
    }

    private async Task<Account> LoadAsync(Guid accountId, CancellationToken cancellationToken) =>
        await accounts.FindAsync(accountId, cancellationToken)
        ?? throw new AccountNotFoundException(accountId);

}
