using Atm.Domain.Exceptions;

namespace Atm.Domain.Accounts;

/// <summary>
/// An account and its rules. Every balance change goes through one of the methods below,
/// and each one records exactly one ledger entry with the balance after the change.
/// </summary>
public sealed class Account
{
    private readonly List<Transaction> _transactions = [];

    private Account(Guid id, string name, string number)
    {
        Id = id;
        Name = name;
        Number = number;
    }

    public Guid Id { get; }

    /// <summary>For example "Checking" or "Savings".</summary>
    public string Name { get; }

    /// <summary>Masked for display, for example "****1234".</summary>
    public string Number { get; }

    public decimal Balance { get; private set; }

    public IReadOnlyList<Transaction> Transactions => _transactions;

    /// <summary>Opens an account at zero. Opening amounts are made as ordinary deposits, so they appear in the ledger.</summary>
    public static Account Open(string name, string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        return new Account(Guid.NewGuid(), name, number);
    }

    public void Deposit(decimal amount, string? description = null)
    {
        AmountRules.Validate(amount);

        Balance += amount;
        Record(TransactionType.Deposit, amount, transferId: null, description);
    }

    public void Withdraw(decimal amount, string? description = null)
    {
        AmountRules.Validate(amount);
        EnsureSufficientFunds(amount);

        Balance -= amount;
        Record(TransactionType.Withdrawal, amount, transferId: null, description);
    }

    /// <summary>
    /// The source leg of a transfer. Only the transfer use case calls this, and it passes the same
    /// <paramref name="transferId"/> to <see cref="TransferIn"/> on the destination account.
    /// </summary>
    public void TransferOut(Guid toAccountId, Guid transferId, decimal amount, string? description = null)
    {
        AmountRules.Validate(amount);
        // Checked before funds, so a self-transfer is reported as such even when the balance is too low.
        if (toAccountId == Id)
        {
            throw new SameAccountTransferException();
        }

        EnsureSufficientFunds(amount);

        Balance -= amount;
        Record(TransactionType.TransferOut, amount, transferId, description);
    }

    /// <summary>The destination leg of a transfer. It pairs with <see cref="TransferOut"/> on the same <paramref name="transferId"/>.</summary>
    public void TransferIn(Guid fromAccountId, Guid transferId, decimal amount, string? description = null)
    {
        AmountRules.Validate(amount);
        if (fromAccountId == Id)
        {
            throw new SameAccountTransferException();
        }

        Balance += amount;
        Record(TransactionType.TransferIn, amount, transferId, description);
    }

    private void EnsureSufficientFunds(decimal amount)
    {
        if (amount > Balance)
        {
            throw new InsufficientFundsException(Balance, amount);
        }
    }

    private void Record(TransactionType type, decimal amount, Guid? transferId, string? description)
    {
        _transactions.Add(new Transaction(
            Guid.NewGuid(),
            Id,
            type,
            amount,
            Balance,
            DateTime.UtcNow,
            transferId,
            description));
    }
}
