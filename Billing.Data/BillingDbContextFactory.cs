using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Billing.Data;

public sealed class BillingDbContextFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    public BillingDbContext CreateDbContext(string[] args)
    {
        var connectionString = args.Length > 0 
            ? args[0] 
            : "Host=localhost;Port=5432;Database=risa_billing;Username=postgres;Password=dev_postgres_123";

        var optionsBuilder = new DbContextOptionsBuilder<BillingDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsHistoryTable("__ef_migrations_history", "billing");
        });

        return new BillingDbContext(optionsBuilder.Options);
    }
}
