namespace Atm.Domain.Exceptions;

/// <summary>Base type for every rule violation raised by the domain. The API maps it to a ProblemDetails response.</summary>
public abstract class DomainException(string message) : Exception(message);
