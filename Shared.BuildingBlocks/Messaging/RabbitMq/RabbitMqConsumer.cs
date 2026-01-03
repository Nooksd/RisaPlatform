using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Kernel.Primitives;
using System.Text.Json;

namespace Shared.BuildingBlocks.Messaging.RabbitMq;

public sealed class RabbitMqConsumer<TEvent, THandler>(IServiceProvider provider, RabbitMqOptions options) : BackgroundService
    where TEvent : class, IIntegrationEvent
    where THandler : IIntegrationEventHandler<TEvent>
{
    private readonly IServiceProvider _provider = provider;
    private readonly RabbitMqOptions _options = options;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = _provider.GetRequiredService<RabbitMqConnection>();
        _channel = connection.Channel;

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var @event = JsonSerializer.Deserialize<TEvent>(ea.Body.Span);

            if (@event is null)
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }

            using var scope = _provider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<THandler>();

            await handler.HandleAsync(@event, stoppingToken);
            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await _channel.BasicConsumeAsync(
            queue: _options.Queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }
}
