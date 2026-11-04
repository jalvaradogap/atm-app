using Atm.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Atm.Infrastructure.Persistence;

public sealed class AtmDbContext(DbContextOptions<AtmDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AtmDbContext).Assembly);
}
