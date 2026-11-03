namespace Atm.Domain.Exceptions;

/// <summary>A withdrawal or outgoing transfer is larger than the account balance (no overdraft).</summary>
public sealed class InsufficientFundsException(decimal balance, decimal requested)
    : DomainException($"Insufficient funds: balance is {balance:0.00}, requested {requested:0.00}.");
