using Atm.Application.Accounts;
using Atm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers EF Core on SQLite and the repository. The connection string is a parameter so
    /// integration tests can point the same registration at a different database.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AtmDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IAccountRepository, AccountRepository>();

        return services;
    }
}
