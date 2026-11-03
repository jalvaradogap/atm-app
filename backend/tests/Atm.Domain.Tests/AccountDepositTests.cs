using Atm.Domain.Accounts;
using Atm.Domain.Exceptions;
using FluentAssertions;

namespace Atm.Domain.Tests;

public class AccountDepositTests
{
    public static TheoryData<decimal> InvalidDepositAmounts => new()
    {
        0m,
        -5m,
        10.001m,
        1_000_000.01m,
    };

    [Fact]
    public void Deposit_IncreasesBalance_AndRecordsOneDepositEntry()
    {
        var account = AccountFactory.WithBalance(0);
        var before = DateTime.UtcNow;

        account.Deposit(100m);

        var after = DateTime.UtcNow;
        account.Balance.Should().Be(100m);
        account.Transactions.Should().ContainSingle();
        var entry = account.Transactions[0];
        entry.Type.Should().Be(TransactionType.Deposit);
        entry.Amount.Should().Be(100m);
        entry.BalanceAfter.Should().Be(100m);
        entry.TransferId.Should().BeNull();
        entry.Description.Should().BeNull();
        entry.OccurredAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Deposit_KeepsTheDescription_WhenOneIsGiven()
    {
        var account = AccountFactory.WithBalance(0);

        account.Deposit(20m, "Paycheck");

        account.Transactions[0].Description.Should().Be("Paycheck");
    }

    [Theory]
    [MemberData(nameof(InvalidDepositAmounts))]
    public void Deposit_RejectsInvalidAmount_AndLeavesBalanceAndLedgerUnchanged(decimal amount)
    {
        var account = AccountFactory.WithBalance(50m);

        var act = () => account.Deposit(amount);

        act.Should().Throw<InvalidAmountException>();
        account.Balance.Should().Be(50m);
        account.Transactions.Should().ContainSingle();
    }
}
