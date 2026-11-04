using System.Net;
using System.Net.Http.Json;
using System.Text;
using Atm.Application.Dtos;
using Atm.Domain.Accounts;
using FluentAssertions;

namespace Atm.Api.Tests;

public class TransfersEndpointTests
{
    [Fact]
    public async Task Transfer_Returns200_UpdatesBothBalances_AndWritesTwoLinkedLegs()
    {
        using var factory = new AtmApiFactory();
        using var client = factory.CreateClient();
        var (checking, savings) = await ApiTestClient.SeededIdsAsync(client);

        var response = await client.PostAsJsonAsync("/api/transfers",
            new TransferRequestBody(checking, savings, 250.50m, "Rent"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TransferResultDto>(ApiTestClient.Json);
        result!.From.Balance.Should().Be(749.50m);
        result.To.Balance.Should().Be(5_250.50m);

        var sourceLedger = await client.GetFromJsonAsync<PagedResult<TransactionDto>>(
            $"/api/accounts/{checking}/transactions?page=1&pageSize=20", ApiTestClient.Json);
        var destinationLedger = await client.GetFromJsonAsync<PagedResult<TransactionDto>>(
            $"/api/accounts/{savings}/transactions?page=1&pageSize=20", ApiTestClient.Json);

        sourceLedger!.Items.Single(t => t.TransferId == result.TransferId).Type.Should().Be(TransactionType.TransferOut);
        destinationLedger!.Items.Single(t => t.TransferId == result.TransferId).Type.Should().Be(TransactionType.TransferIn);
    }

    [Fact]
    public async Task Transfer_ToTheSameAccount_Returns422_AndChangesNothing()
    {
        using var factory = new AtmApiFactory();
        using var client = factory.CreateClient();
        var (checking, _) = await ApiTestClient.SeededIdsAsync(client);

        var response = await client.PostAsJsonAsync("/api/transfers",
            new TransferRequestBody(checking, checking, 10m, null));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await ApiTestClient.GetAccountAsync(client, checking)).Balance.Should().Be(1_000m);
    }

    [Fact]
    public async Task Transfer_FromAnUnknownAccount_Returns404()
    {
        using var factory = new AtmApiFactory();
        using var client = factory.CreateClient();
        var (_, savings) = await ApiTestClient.SeededIdsAsync(client);

        var response = await client.PostAsJsonAsync("/api/transfers",
            new TransferRequestBody(Guid.NewGuid(), savings, 10m, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Transfer_WithoutADestination_Returns400()
    {
        using var factory = new AtmApiFactory();
        using var client = factory.CreateClient();
        var (checking, _) = await ApiTestClient.SeededIdsAsync(client);

        var response = await client.PostAsync("/api/transfers",
            new StringContent($"{{\"fromAccountId\":\"{checking}\",\"amount\":10}}", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

/// <summary>Transfer body, in the wire format the API expects.</summary>
internal sealed record TransferRequestBody(Guid FromAccountId, Guid ToAccountId, decimal Amount, string? Description);
