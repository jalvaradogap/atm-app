using System.Net;
using System.Net.Http.Json;
using Atm.Application.Accounts;
using Atm.Domain.Accounts;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Api.Tests;

/// <summary>
/// A stale write is rejected by the Version check inside the database, which EF reports as
/// DbUpdateConcurrencyException. A real race between two HTTP requests can't be made to hit that
/// exact moment reliably, so the exception is raised directly. The persistence tests already prove
/// that a stale save really fails.
/// </summary>
public class ConcurrencyEndpointTests
{
    [Fact]
    public async Task ConcurrentUpdate_Returns409_WithAProblemResponse_AndNoInternalDetails()
    {
        using var factory = new AtmApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IAccountRepository, StaleWriteAccountRepository>()));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/accounts/{Guid.NewGuid()}/withdrawals",
            new AmountRequestBody(10m, null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var (status, title) = await ApiTestClient.ReadProblemAsync(response);
        status.Should().Be(409);
        title.Should().Be("Conflict");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("stale version");
    }
}

/// <summary>Loads an account normally, then fails the save the way a stale Version does.</summary>
internal sealed class StaleWriteAccountRepository : IAccountRepository
{
    private readonly Account _account = CreateFundedAccount();

    public Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Account>>([_account]);

    public Task<Account?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult<Account?>(_account);

    public Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetTransactionsPageAsync(
        Guid accountId, int page, int pageSize, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        throw new DbUpdateConcurrencyException("stale version");

    private static Account CreateFundedAccount()
    {
        var account = Account.Open("Checking", "****1234");
        account.Deposit(500m);
        return account;
    }
}
