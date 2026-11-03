using Atm.Domain.Accounts;
using Atm.Domain.Exceptions;
using FluentAssertions;

namespace Atm.Domain.Tests;

/// <summary>Tests the amount rule directly. The per-operation tests below check that each operation uses it.</summary>
public class AmountRulesTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(5.00)]
    [InlineData(10.50)]
    [InlineData(1_000_000)]
    public void Validate_AcceptsValidAmounts(double amount)
    {
        var act = () => AmountRules.Validate((decimal)amount);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_RejectsZeroAndNegative(double amount)
    {
        var act = () => AmountRules.Validate((decimal)amount);

        act.Should().Throw<InvalidAmountException>();
    }

    [Fact]
    public void Validate_RejectsMoreThanTwoDecimals()
    {
        var act = () => AmountRules.Validate(10.001m);

        act.Should().Throw<InvalidAmountException>();
    }

    [Fact]
    public void Validate_RejectsAmountAboveMaximum()
    {
        var act = () => AmountRules.Validate(1_000_000.01m);

        act.Should().Throw<InvalidAmountException>();
    }
}
