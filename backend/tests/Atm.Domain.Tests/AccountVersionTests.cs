using Atm.Domain.Exceptions;
using FluentAssertions;

namespace Atm.Domain.Tests;

public class AccountVersionTests
{
    [Fact]
    public void Version_IncrementsOnEveryBalanceChange()
    {
        var account = AccountFactory.WithBalance(0m);
        var startVersion = account.Version;

        account.Deposit(100m);
        account.Withdraw(40m);

        account.Version.Should().Be(startVersion + 2);
    }

    [Fact]
    public void Version_DoesNotChange_WhenAnOperationIsRejected()
    {
        var account = AccountFactory.WithBalance(50m);
        var versionBefore = account.Version;

        var act = () => account.Withdraw(500m);

        act.Should().Throw<InsufficientFundsException>();
        account.Version.Should().Be(versionBefore);
    }
}
