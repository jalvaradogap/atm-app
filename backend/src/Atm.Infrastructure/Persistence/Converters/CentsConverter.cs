using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Atm.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores a money <c>decimal</c> as an integer number of cents. SQLite has no decimal type, and
/// REAL would introduce binary rounding error. Integers are exact and sort correctly.
/// </summary>
public sealed class CentsConverter() : ValueConverter<decimal, long>(
    amount => ToCents(amount),
    cents => FromCents(cents))
{
    public static readonly CentsConverter Instance = new();

    private static long ToCents(decimal amount)
    {
        var cents = amount * 100m;

        // The domain only allows 2 decimal places. Failing here is safer than silently dropping money.
        if (cents != decimal.Truncate(cents))
        {
            throw new InvalidOperationException($"Amount {amount} cannot be stored exactly in cents.");
        }

        return checked((long)cents);
    }

    private static decimal FromCents(long cents) => cents / 100m;
}
