using System.Net;
using Atm.Application.Accounts;
using Atm.Domain.Accounts;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Api.Tests;

public class UnexpectedErrorEndpointTests
{
    private const string InternalMessage = "secret connection string: Data Source=/var/private/atm.db";

    [Fact]
    public async Task UnexpectedException_Returns500_WithAGenericProblem_AndNoInternalDetails()
    {
        using var factory = new AtmApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped<IAccountRepository, ThrowingAccountRepository>()));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();
        var (status, title) = await ApiTestClient.ReadProblemAsync(response);
        status.Should().Be(500);
        title.Should().Be("An unexpected error occurred");
        body.Should().NotContain(InternalMessage);
        body.Should().NotContain("   at ");
    }
}

/// <summary>Stands in for a repository that fails unexpectedly, such as a lost database connection.</summary>
internal sealed class ThrowingAccountRepository : IAccountRepository
{
    public Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken) =>
        throw new InvalidOperationException("secret connection string: Data Source=/var/private/atm.db");

    public Task<Account?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetTransactionsPageAsync(
        Guid accountId, int page, int pageSize, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
