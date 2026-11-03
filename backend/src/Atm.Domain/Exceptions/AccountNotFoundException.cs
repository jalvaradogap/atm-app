namespace Atm.Domain.Exceptions;

/// <summary>Defined here so every layer shares one type. The application layer throws it when a lookup finds nothing.</summary>
public sealed class AccountNotFoundException(Guid accountId)
    : DomainException($"Account {accountId} was not found.");
