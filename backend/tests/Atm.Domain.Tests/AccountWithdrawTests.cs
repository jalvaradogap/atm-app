using Atm.Domain.Accounts;
using Atm.Domain.Exceptions;
using FluentAssertions;

namespace Atm.Domain.Tests;

public class AccountWithdrawTests
{
    public static TheoryData<decimal> InvalidWithdrawAmounts => new()
    {
        0m,
        -5m,
        10.001m,
    };

    [Fact]
    public void Withdraw_DecreasesBalance_AndRecordsOneWithdrawalEntry()
    {
        var account = AccountFactory.WithBalance(100m);

        account.Withdraw(40m);

        account.Balance.Should().Be(60m);
        account.Transactions.Should().HaveCount(2);
        var entry = account.Transactions[1];
        entry.Type.Should().Be(TransactionType.Withdrawal);
        entry.Amount.Should().Be(40m);
        entry.BalanceAfter.Should().Be(60m);
    }

    [Fact]
    public void Withdraw_AllowsExactBalance()
    {
        var account = AccountFactory.WithBalance(100m);

        account.Withdraw(100m);

        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void Withdraw_RejectsOverdraft_AndLeavesBalanceAndLedgerUnchanged()
    {
        var account = AccountFactory.WithBalance(100m);

        var act = () => account.Withdraw(100.01m);

        act.Should().Throw<InsufficientFundsException>();
        account.Balance.Should().Be(100m);
        account.Transactions.Should().ContainSingle();
    }

    [Theory]
    [MemberData(nameof(InvalidWithdrawAmounts))]
    public void Withdraw_RejectsInvalidAmount_AndLeavesBalanceAndLedgerUnchanged(decimal amount)
    {
        var account = AccountFactory.WithBalance(100m);

        var act = () => account.Withdraw(amount);

        act.Should().Throw<InvalidAmountException>();
        account.Balance.Should().Be(100m);
        account.Transactions.Should().ContainSingle();
    }
}
