namespace Shared.BuildingBlocks.Messaging.RabbitMq;

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = default!;
    public int Port { get; init; } = 5672;
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string Exchange { get; init; } = "billing.events";
    public string Queue { get; init; } = default!;
    public string DeadLetterExchange { get; init; } = "billing.events.dlx";
}