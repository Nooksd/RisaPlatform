using Shared.Kernel.Primitives;

namespace Shared.Contracts.Billing;

public sealed record TenantDataDeletionCommand(
    Guid TenantId,
    DateTime RequestedAt,
    string Reason
) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}