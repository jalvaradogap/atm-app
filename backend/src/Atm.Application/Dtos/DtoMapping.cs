using Atm.Domain.Accounts;

namespace Atm.Application.Dtos;

/// <summary>Plain static mapping from domain objects to DTOs. Kept explicit so every field is visible.</summary>
public static class DtoMapping
{
    public static AccountDto ToDto(Account account) =>
        new(account.Id, account.Name, account.Number, account.Balance);

    public static TransactionDto ToDto(Transaction transaction) =>
        new(
            transaction.Id,
            transaction.AccountId,
            transaction.Type,
            transaction.Amount,
            transaction.BalanceAfter,
            transaction.OccurredAt,
            transaction.TransferId,
            transaction.Description);
}
