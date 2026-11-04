using System.Net;
using System.Net.Http.Json;
using System.Text;
using Atm.Application.Dtos;
using Atm.Domain.Accounts;
using FluentAssertions;

namespace Atm.Api.Tests;

public class AccountsEndpointTests
{
    [Fact]
    public async Task ListAccounts_Returns200_WithBothSeededAccounts()
    {
        using var factory = new AtmApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = await ApiTestClient.ListAccountsAsync(client);
        accounts.Select(a => (a.Name, a.Balance)).Should().BeEquivalentTo(new[] { ("Checking", 1_000m), ("Savings", 5_000m) });
    }

    [Fact]
    public async Task Deposit_Returns200_AndUpdatesTheBalance()
    {
        using var factory = new AtmApiFactory();
        using var client = factory.CreateClient();
        var (checking, _) = await ApiTestClient.SeededIdsAsync(client);

        var response = await client.PostAsJsonAsync($"/api/accounts/{checking}/deposits", new AmountRequestBody(100m, "Cash"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<AccountDto>(ApiTestClient.Json);
        updated!.Balance.Should().Be(1_100m);
        (await ApiTestClient.GetAccountAsync(client, checking)).Balance.Should().Be(1_100m);
    }

    [Fact]
    public async Task Withdraw_MoreThanTheBalance_Returns422_AndLeavesTheBalanceUnchanged()
    {
        using var factory = new AtmApiFactory();
        using var client = factory.CreateClient();
        var (checking, _) = await ApiTestClient.SeededIdsAsync(client);

        var response = await client.PostAsJsonAsync($"/api/accounts/{checking}/withdrawals", new AmountRequestBody(5_000m, null));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        (await ApiTestClient.GetAccountAsync(client, checking)).Balance.Should().Be(1_000m);
    }

    [Fact]
    public async Task Transactions_ReturnTheNewestEntryFirst_WithTheTotalCount()
    {
        using var factory = new AtmApiFactory();
        using var client = factory.CreateClient();
        var (checking, _) = await ApiTestClient.SeededIdsAsync(client);
        await client.PostAsJsonAsync($"/api/accounts/{checking}/deposits", new AmountRequestBody(25m, "Latest"));

        var page = await client.GetFromJsonAsync<PagedResult<TransactionDto>>(
            $"/api/accounts/{checking}/transactions?page=1&pageSize=20", ApiTestClient.Json);

        page!.TotalCount.Should().Be(2);
        page.Items.First().Description.Should().Be("Latest");
        page.Items.First().Type.Should().Be(TransactionType.Deposit);
    }

    [Fact]
    public async Task UnknownAccount_Returns404_WithAProblemResponse()
    {
        using var factory = new AtmApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/accounts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var (status, _) = await ApiTestClient.ReadProblemAsync(response);
        status.Should().Be(404);
    }

    [Fact]
    public async Task Deposit_WithANegativeAmount_Returns400_AndChangesNothing()
    {
        using var factory = new AtmApiFactory();
        using var client = factory.CreateClient();
        var (checking, _) = await ApiTestClient.SeededIdsAsync(client);

        var response = await client.PostAsJsonAsync($"/api/accounts/{checking}/deposits", new AmountRequestBody(-5m, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiTestClient.GetAccountAsync(client, checking)).Balance.Should().Be(1_000m);
    }

    [Fact]
    public async Task Deposit_WithAMissingAmount_Returns400()
    {
        using var factory = new AtmApiFactory();
        using var client = factory.CreateClient();
        var (checking, _) = await ApiTestClient.SeededIdsAsync(client);

        var response = await client.PostAsync(
            $"/api/accounts/{checking}/deposits",
            new StringContent("{\"description\":\"no amount\"}", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

/// <summary>Deposit and withdrawal body, in the wire format the API expects.</summary>
internal sealed record AmountRequestBody(decimal Amount, string? Description);
