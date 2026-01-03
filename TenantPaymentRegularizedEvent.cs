public record TenantPaymentRegularizedEvent(
    Guid TenantId,
    string[] AllowedModules
) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
