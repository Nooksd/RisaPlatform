using Shared.Kernel.Primitives;

namespace Shared.Contracts.Billing;

public sealed record TenantPaymentConfirmedEvent(
    Guid TenantId,
    DateTime PayedAt,
    TimeSpan Duration,
    string[] AllowedModules
) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}