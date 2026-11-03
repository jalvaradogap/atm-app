namespace Atm.Domain.Accounts;

/// <summary>
/// One ledger entry. Immutable, and created only by <see cref="Account"/>, so the
/// balance can never change without a matching entry.
/// </summary>
public sealed class Transaction
{
    internal Transaction(
        Guid id,
        Guid accountId,
        TransactionType type,
        decimal amount,
        decimal balanceAfter,
        DateTime occurredAt,
        Guid? transferId,
        string? description)
    {
        Id = id;
        AccountId = accountId;
        Type = type;
        Amount = amount;
        BalanceAfter = balanceAfter;
        OccurredAt = occurredAt;
        TransferId = transferId;
        Description = description;
    }

    public Guid Id { get; }

    public Guid AccountId { get; }

    public TransactionType Type { get; }

    /// <summary>Always positive. The <see cref="Type"/> gives the direction.</summary>
    public decimal Amount { get; }

    public decimal BalanceAfter { get; }

    /// <summary>UTC time the entry was recorded.</summary>
    public DateTime OccurredAt { get; }

    /// <summary>Shared by the two legs of one transfer. Null for deposits and withdrawals.</summary>
    public Guid? TransferId { get; }

    public string? Description { get; }
}
