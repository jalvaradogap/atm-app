using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Atm.Api.Tests;

/// <summary>
/// Hosts the real API against a private in-memory SQLite database. Create one per test, so every test
/// starts from freshly seeded balances. The connection is opened here and kept open, because an in-memory
/// database disappears when its last connection closes.
/// </summary>
public sealed class AtmApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _keepAlive;

    public AtmApiFactory()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Atm", _connectionString);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _keepAlive.Dispose();
        }

        base.Dispose(disposing);
    }
}
