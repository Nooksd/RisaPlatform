using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Billing.Data;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<ModuleQuantityDiscount> ModuleQuantityDiscounts => Set<ModuleQuantityDiscount>();
    public DbSet<TenantBilling> TenantBillings => Set<TenantBilling>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionModule> SubscriptionModules => Set<SubscriptionModule>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("billing");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
    }
}
