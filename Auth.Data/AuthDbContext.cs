using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<TenantAccount> TenantAccounts => Set<TenantAccount>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<ModuleAccess> ModuleAccesses => Set<ModuleAccess>();
    public DbSet<PublicUser> PublicUsers => Set<PublicUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
