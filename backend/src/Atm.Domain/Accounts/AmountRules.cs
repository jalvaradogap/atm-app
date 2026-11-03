using Atm.Domain.Exceptions;

namespace Atm.Domain.Accounts;

/// <summary>The single rule for what counts as a valid money amount. Every operation calls it first.</summary>
public static class AmountRules
{
    public const decimal MaxAmount = 1_000_000m;

    public static void Validate(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidAmountException("Amount must be greater than zero.");
        }

        // Rounding and comparing, rather than counting decimal places, means 5.00 is valid and 10.001 is not.
        if (decimal.Round(amount, 2) != amount)
        {
            throw new InvalidAmountException("Amount can have at most 2 decimal places.");
        }

        if (amount > MaxAmount)
        {
            throw new InvalidAmountException($"Amount cannot exceed {MaxAmount:N0}.");
        }
    }
}
