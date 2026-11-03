namespace Atm.Domain.Exceptions;

/// <summary>A transfer names the same account as both source and destination.</summary>
public sealed class SameAccountTransferException()
    : DomainException("Source and destination accounts must be different.");
