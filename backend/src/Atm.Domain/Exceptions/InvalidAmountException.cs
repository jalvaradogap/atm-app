namespace Atm.Domain.Exceptions;

/// <summary>The amount is zero, negative, has more than 2 decimals, or is above the maximum.</summary>
public sealed class InvalidAmountException(string message) : DomainException(message);
