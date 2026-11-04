using System.Globalization;
using Atm.Infrastructure.Persistence.Converters;
using FluentAssertions;

namespace Atm.Infrastructure.Tests;

public class CentsConverterTests
{
    [Theory]
    [InlineData("0.10", 10)]
    [InlineData("999.99", 99_999)]
    [InlineData("1000000", 100_000_000)]
    public void ToProvider_StoresWholeCents(string amount, long expectedCents)
    {
        var cents = CentsConverter.Instance.ConvertToProvider(Parse(amount));

        cents.Should().Be(expectedCents);
    }

    [Theory]
    [InlineData("0.10")]
    [InlineData("999.99")]
    [InlineData("0")]
    public void RoundTrip_ReturnsTheOriginalAmount(string amount)
    {
        var original = Parse(amount);

        var roundTripped = CentsConverter.Instance.ConvertFromProvider(
            CentsConverter.Instance.ConvertToProvider(original));

        roundTripped.Should().Be(original);
    }

    [Fact]
    public void ToProvider_RejectsAmountsThatCannotBeStoredExactly()
    {
        var act = () => CentsConverter.Instance.ConvertToProvider(0.001m);

        act.Should().Throw<InvalidOperationException>();
    }

    private static decimal Parse(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
