using Shared.Kernel.Primitives;

namespace Shared.Contracts.Billing;

public sealed record TenantGracePeriodGrantedEvent(
    Guid TenantId,
    DateTime GrantedAt,
    DateTime ExpiresAt,
    string[] AllowedModules
) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
