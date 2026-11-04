using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Atm.Application.Dtos;

namespace Atm.Api.Tests;

/// <summary>Shared helpers for the endpoint tests: reading the seeded accounts and parsing responses.</summary>
internal static class ApiTestClient
{
    // Matches the API's JSON settings: camelCase names and enums as strings.
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<IReadOnlyList<AccountDto>> ListAccountsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<IReadOnlyList<AccountDto>>("/api/accounts", Json))!;

    /// <summary>Finds Checking and Savings by name, so tests don't depend on list order.</summary>
    public static async Task<(Guid Checking, Guid Savings)> SeededIdsAsync(HttpClient client)
    {
        var accounts = await ListAccountsAsync(client);
        return (accounts.Single(a => a.Name == "Checking").Id, accounts.Single(a => a.Name == "Savings").Id);
    }

    public static async Task<AccountDto> GetAccountAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<AccountDto>($"/api/accounts/{id}", Json))!;

    /// <summary>Reads the status and title from a problem+json body.</summary>
    public static async Task<(int Status, string Title)> ReadProblemAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        return (root.GetProperty("status").GetInt32(), root.GetProperty("title").GetString()!);
    }
}
