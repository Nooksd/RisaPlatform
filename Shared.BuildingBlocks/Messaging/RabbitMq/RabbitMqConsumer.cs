using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Kernel.Primitives;
using System.Text.Json;

namespace Shared.BuildingBlocks.Messaging.RabbitMq;

public sealed class RabbitMqConsumer<TEvent, THandler>(
    IServiceProvider provider,
    RabbitMqOptions options,
    ILogger<RabbitMqConsumer<TEvent, THandler>> logger) : BackgroundService
    where TEvent : class, IIntegrationEvent
    where THandler : IIntegrationEventHandler<TEvent>
{
    private readonly IServiceProvider _provider = provider;
    private readonly RabbitMqOptions _options = options;
    private readonly ILogger _logger = logger;
    private IChannel? _channel;

    private const int MaxRetryCount = 3;
    private const string RetryCountHeader = "x-retry-count";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var connection = _provider.GetRequiredService<RabbitMqConnection>();
            _channel = connection.Channel;

            await _channel.QueueDeclareAsync(
                queue: _options.Queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    { "x-dead-letter-exchange", _options.DeadLetterExchange },
                    { "x-dead-letter-routing-key", $"{_options.Queue}.failed" }
                },
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                var retryCount = GetRetryCount(ea.BasicProperties);

                try
                {
                    var @event = JsonSerializer.Deserialize<TEvent>(ea.Body.Span);

                    if (@event is null)
                    {
                        _logger.LogError(
                            "Failed to deserialize event from queue {Queue}. DeliveryTag: {DeliveryTag}",
                            _options.Queue,
                            ea.DeliveryTag);

                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                        return;
                    }

                    _logger.LogInformation(
                        "Processing event {EventType} with ID {EventId} from queue {Queue}",
                        typeof(TEvent).Name,
                        @event.EventId,
                        _options.Queue);

                    using var scope = _provider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<THandler>();

                    await handler.HandleAsync(@event, stoppingToken);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);

                    _logger.LogInformation(
                        "Successfully processed event {EventType} with ID {EventId}",
                        typeof(TEvent).Name,
                        @event.EventId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing event from queue {Queue}. DeliveryTag: {DeliveryTag}, RetryCount: {RetryCount}",
                        _options.Queue,
                        ea.DeliveryTag,
                        retryCount);

                    if (retryCount >= MaxRetryCount)
                    {
                        _logger.LogWarning(
                            "Max retry count reached for message in queue {Queue}. Sending to DLQ. DeliveryTag: {DeliveryTag}",
                            _options.Queue,
                            ea.DeliveryTag);

                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Requeuing message for retry {RetryCount}/{MaxRetry} in queue {Queue}",
                            retryCount + 1,
                            MaxRetryCount,
                            _options.Queue);

                        await RequeueWithRetryCount(_channel, ea, retryCount + 1);
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _options.Queue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "RabbitMQ consumer started for queue {Queue} listening to events of type {EventType}",
                _options.Queue,
                typeof(TEvent).Name);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RabbitMQ consumer for queue {Queue} is stopping", _options.Queue);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "Fatal error in RabbitMQ consumer for queue {Queue}",
                _options.Queue);
            throw;
        }
    }

    private static int GetRetryCount(IReadOnlyBasicProperties? properties)
    {
        if (properties?.Headers is null)
            return 0;

        if (properties.Headers.TryGetValue(RetryCountHeader, out var value))
        {
            return value switch
            {
                int intValue => intValue,
                byte[] bytes => BitConverter.ToInt32(bytes, 0),
                _ => 0
            };
        }

        return 0;
    }

    private async Task RequeueWithRetryCount(IChannel channel, BasicDeliverEventArgs ea, int newRetryCount)
    {
        var properties = new BasicProperties
        {
            Persistent = true,
            Headers = new Dictionary<string, object?>
            {
                { RetryCountHeader, newRetryCount }
            }
        };

        if (ea.BasicProperties.Headers is not null)
        {
            foreach (var header in ea.BasicProperties.Headers)
            {
                if (header.Key != RetryCountHeader)
                {
                    properties.Headers[header.Key] = header.Value;
                }
            }
        }

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: _options.Queue,
            mandatory: true,
            basicProperties: properties,
            body: ea.Body,
            cancellationToken: default);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ consumer for queue {Queue}", _options.Queue);
        await base.StopAsync(cancellationToken);
    }
}