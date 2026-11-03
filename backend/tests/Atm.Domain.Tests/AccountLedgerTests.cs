using Atm.Domain.Accounts;
using FluentAssertions;

namespace Atm.Domain.Tests;

public class AccountLedgerTests
{
    [Fact]
    public void BalanceAfter_MatchesRunningBalance_AcrossASequenceOfOperations()
    {
        var account = AccountFactory.WithBalance(0m);

        account.Deposit(500m);
        account.Withdraw(120m);
        account.Deposit(30.25m);

        account.Transactions.Select(t => t.BalanceAfter).Should().Equal(500m, 380m, 410.25m);
    }

    [Fact]
    public void Balance_EqualsSumOfSignedLedgerEntries()
    {
        var account = AccountFactory.WithBalance(0m);
        var other = AccountFactory.WithBalance(0m);

        account.Deposit(500m);
        account.Withdraw(120m);
        account.TransferOut(other.Id, Guid.NewGuid(), 80m);
        account.Deposit(30.25m);

        var signedSum = account.Transactions.Sum(t => t.Type is TransactionType.Deposit or TransactionType.TransferIn
            ? t.Amount
            : -t.Amount);
        signedSum.Should().Be(account.Balance);
    }
}
