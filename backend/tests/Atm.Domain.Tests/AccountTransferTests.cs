using Atm.Domain.Accounts;
using Atm.Domain.Exceptions;
using FluentAssertions;

namespace Atm.Domain.Tests;

public class AccountTransferTests
{
    public static TheoryData<decimal> InvalidTransferAmounts => new()
    {
        0m,
        -5m,
        10.001m,
    };

    [Fact]
    public void TransferOut_DecreasesSourceBalance_AndRecordsOneEntry()
    {
        var source = AccountFactory.WithBalance(100m);
        var destination = AccountFactory.WithBalance(0m);
        var transferId = Guid.NewGuid();

        source.TransferOut(destination.Id, transferId, 30m);

        source.Balance.Should().Be(70m);
        var entry = source.Transactions[1];
        entry.Type.Should().Be(TransactionType.TransferOut);
        entry.Amount.Should().Be(30m);
        entry.BalanceAfter.Should().Be(70m);
        entry.TransferId.Should().Be(transferId);
    }

    [Fact]
    public void TransferIn_IncreasesDestinationBalance_AndRecordsOneEntry()
    {
        var source = AccountFactory.WithBalance(100m);
        var destination = AccountFactory.WithBalance(0m);
        var transferId = Guid.NewGuid();

        destination.TransferIn(source.Id, transferId, 30m);

        destination.Balance.Should().Be(30m);
        var entry = destination.Transactions[0];
        entry.Type.Should().Be(TransactionType.TransferIn);
        entry.Amount.Should().Be(30m);
        entry.BalanceAfter.Should().Be(30m);
        entry.TransferId.Should().Be(transferId);
    }

    [Fact]
    public void BothLegs_OfOneTransfer_ShareTheSameTransferId()
    {
        var source = AccountFactory.WithBalance(100m);
        var destination = AccountFactory.WithBalance(0m);
        var transferId = Guid.NewGuid();

        source.TransferOut(destination.Id, transferId, 25m);
        destination.TransferIn(source.Id, transferId, 25m);

        source.Transactions[1].TransferId.Should().Be(destination.Transactions[0].TransferId);
    }

    [Fact]
    public void TransferOut_AllowsExactBalance()
    {
        var source = AccountFactory.WithBalance(100m);
        var destination = AccountFactory.WithBalance(0m);

        source.TransferOut(destination.Id, Guid.NewGuid(), 100m);

        source.Balance.Should().Be(0m);
    }

    [Fact]
    public void TransferOut_RejectsOverdraft_AndLeavesSourceUnchanged()
    {
        var source = AccountFactory.WithBalance(100m);
        var destination = AccountFactory.WithBalance(0m);

        var act = () => source.TransferOut(destination.Id, Guid.NewGuid(), 150m);

        act.Should().Throw<InsufficientFundsException>();
        source.Balance.Should().Be(100m);
        source.Transactions.Should().ContainSingle();
    }

    [Fact]
    public void TransferOut_ToSameAccount_IsRejected_AndLeavesBalanceUnchanged()
    {
        var account = AccountFactory.WithBalance(100m);

        var act = () => account.TransferOut(account.Id, Guid.NewGuid(), 30m);

        act.Should().Throw<SameAccountTransferException>();
        account.Balance.Should().Be(100m);
        account.Transactions.Should().ContainSingle();
    }

    [Fact]
    public void TransferOut_ToSameAccount_IsReportedBeforeInsufficientFunds()
    {
        var account = AccountFactory.WithBalance(0m);

        var act = () => account.TransferOut(account.Id, Guid.NewGuid(), 50m);

        act.Should().Throw<SameAccountTransferException>();
    }

    [Fact]
    public void TransferIn_FromSameAccount_IsRejected_AndLeavesBalanceUnchanged()
    {
        var account = AccountFactory.WithBalance(100m);

        var act = () => account.TransferIn(account.Id, Guid.NewGuid(), 30m);

        act.Should().Throw<SameAccountTransferException>();
        account.Balance.Should().Be(100m);
    }

    [Theory]
    [MemberData(nameof(InvalidTransferAmounts))]
    public void TransferOut_RejectsInvalidAmount_AndLeavesBothAccountsUnchanged(decimal amount)
    {
        var source = AccountFactory.WithBalance(100m);
        var destination = AccountFactory.WithBalance(0m);

        var act = () => source.TransferOut(destination.Id, Guid.NewGuid(), amount);

        act.Should().Throw<InvalidAmountException>();
        source.Balance.Should().Be(100m);
        destination.Balance.Should().Be(0m);
        source.Transactions.Should().ContainSingle();
        destination.Transactions.Should().BeEmpty();
    }
}
