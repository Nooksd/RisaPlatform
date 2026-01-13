using Billing.Data.Repositories;
using Billing.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Billing.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBillingData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BillingDb")
            ?? throw new InvalidOperationException("Billing database connection string not found");

        services.AddDbContext<BillingDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(3);
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "billing");
            });

            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        });

        // Repositories
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<ITenantBillingRepository, TenantBillingRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddBillingDataForMigrations(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<BillingDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "billing");
            });
        });

        return services;
    }
}
