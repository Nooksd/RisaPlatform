using Billing.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Billing.Data.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BillingDbContext _context;
    private IDbContextTransaction? _transaction;
    
    private IModuleRepository? _modules;
    private ITenantBillingRepository? _tenantBillings;
    private ISubscriptionRepository? _subscriptions;
    private IPaymentRepository? _payments;
    private ITenantDeletionTrackingRepository? _deletionTrackings;
    private IEmailNotificationLogRepository? _emailNotificationLogs;
    
    public UnitOfWork(BillingDbContext context)
    {
        _context = context;
    }
    
    public IModuleRepository Modules 
        => _modules ??= new ModuleRepository(_context);
        
    public ITenantBillingRepository TenantBillings 
        => _tenantBillings ??= new TenantBillingRepository(_context);
        
    public ISubscriptionRepository Subscriptions 
        => _subscriptions ??= new SubscriptionRepository(_context);
        
    public IPaymentRepository Payments 
        => _payments ??= new PaymentRepository(_context);
        
    public ITenantDeletionTrackingRepository DeletionTrackings 
        => _deletionTrackings ??= new TenantDeletionTrackingRepository(_context);
        
    public IEmailNotificationLogRepository EmailNotificationLogs 
        => _emailNotificationLogs ??= new EmailNotificationLogRepository(_context);
    
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
        
    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(ct);
    }
    
    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    
    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
