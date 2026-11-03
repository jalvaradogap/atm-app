using Atm.Domain.Accounts;

namespace Atm.Domain.Tests;

/// <summary>Builds accounts in a known state, so each test starts from one clear balance.</summary>
internal static class AccountFactory
{
    public static Account WithBalance(decimal balance)
    {
        var account = Account.Open("Checking", "****1234");
        if (balance > 0)
        {
            account.Deposit(balance);
        }

        return account;
    }
}
