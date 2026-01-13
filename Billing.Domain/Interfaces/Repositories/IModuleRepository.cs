using Billing.Domain.Entities;

namespace Billing.Domain.Interfaces.Repositories;

public interface IModuleRepository : IRepository<Module>
{
    Task<Module?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IEnumerable<Module>> GetActiveModulesAsync(CancellationToken ct = default);
    Task<IEnumerable<Module>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken ct = default);
    Task<Module?> GetWithQuantityDiscountsAsync(Guid id, CancellationToken ct = default);
}
