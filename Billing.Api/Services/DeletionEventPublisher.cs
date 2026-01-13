using RabbitMQ.Client;
using Shared.Contracts.Billing;
using System.Text.Json;

namespace Billing.Api.Services;

/// <summary>
/// Publisher específico para eventos de deletion que usa o exchange fanout tenant.deletion
/// </summary>
public interface IDeletionEventPublisher
{
    Task PublishDeletionCommandAsync(TenantDataDeletionCommand command, CancellationToken ct = default);
}

public sealed class DeletionEventPublisher(
    Shared.BuildingBlocks.Messaging.RabbitMq.RabbitMqConnection connection,
    ILogger<DeletionEventPublisher> logger) : IDeletionEventPublisher
{
    private readonly IChannel _channel = connection.Channel;
    private readonly ILogger<DeletionEventPublisher> _logger = logger;
    private const string DeletionExchange = "tenant.deletion";

    public async Task PublishDeletionCommandAsync(TenantDataDeletionCommand command, CancellationToken ct = default)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(command);

        await _channel.BasicPublishAsync(
            exchange: DeletionExchange,
            routingKey: "",
            body: body,
            cancellationToken: ct);

        _logger.LogInformation(
            "Published TenantDataDeletionCommand to fanout exchange for tenant {TenantId}",
            command.TenantId);
    }
}
