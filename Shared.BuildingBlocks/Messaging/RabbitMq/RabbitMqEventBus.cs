using RabbitMQ.Client;
using Shared.Kernel.Primitives;
using System.Text.Json;

namespace Shared.BuildingBlocks.Messaging.RabbitMq;

public sealed class RabbitMqEventBus(RabbitMqConnection connection, RabbitMqOptions options) : IEventBus
{
    private readonly IChannel _channel = connection.Channel;
    private readonly RabbitMqOptions _options = options;

    public async Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(@event);

        await _channel.BasicPublishAsync(
            exchange: _options.Exchange,
            routingKey: typeof(TEvent).Name,
            body: body,
            cancellationToken: ct);
    }
}
