using Atm.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Atm.Infrastructure.Tests;

/// <summary>
/// A private in-memory SQLite database for one test. An in-memory database exists only while a
/// connection to it is open, so one connection is kept open for the test's lifetime. Each
/// <see cref="CreateContext"/> call gets its own connection, which lets tests simulate two
/// separate requests.
/// </summary>
internal sealed class TestDatabase : IDisposable
{
    private readonly string _connectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _keepAlive;

    public TestDatabase()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();

        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    public AtmDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AtmDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        return new AtmDbContext(options);
    }

    public void Dispose() => _keepAlive.Dispose();
}
