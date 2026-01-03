using Shared.Kernel.Primitives;

namespace Shared.Contracts.Billing;

public sealed record TenantGracePeriodGrantedEvent(
    Guid TenantId,
    DateTime GrantedAt,
    TimeSpan Duration,
    string[] AllowedModules
) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
