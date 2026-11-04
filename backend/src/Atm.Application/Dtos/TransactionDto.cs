using Atm.Domain.Accounts;

namespace Atm.Application.Dtos;

public sealed record TransactionDto(
    Guid Id,
    Guid AccountId,
    TransactionType Type,
    decimal Amount,
    decimal BalanceAfter,
    DateTime OccurredAt,
    Guid? TransferId,
    string? Description);
