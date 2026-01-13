using System.Linq.Expressions;
using Billing.Domain.Entities;
using Billing.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Billing.Data.Repositories;

public class Repository<T> : IRepository<T> where T : Entity
{
    protected readonly BillingDbContext Context;
    protected readonly DbSet<T> DbSet;
    
    public Repository(BillingDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }
    
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);
        
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await DbSet.ToListAsync(ct);
        
    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate, 
        CancellationToken ct = default)
        => await DbSet.Where(predicate).ToListAsync(ct);
        
    public virtual async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate, 
        CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(predicate, ct);
        
    public virtual async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate, 
        CancellationToken ct = default)
        => await DbSet.AnyAsync(predicate, ct);
        
    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null, 
        CancellationToken ct = default)
        => predicate is null 
            ? await DbSet.CountAsync(ct) 
            : await DbSet.CountAsync(predicate, ct);
            
    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await DbSet.AddAsync(entity, ct);
        
    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        => await DbSet.AddRangeAsync(entities, ct);
        
    public virtual void Update(T entity)
        => DbSet.Update(entity);
        
    public virtual void Remove(T entity)
        => DbSet.Remove(entity);
        
    public virtual void RemoveRange(IEnumerable<T> entities)
        => DbSet.RemoveRange(entities);
}
