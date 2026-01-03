using RabbitMQ.Client;

namespace Shared.BuildingBlocks.Messaging.RabbitMq;

public sealed class RabbitMqConnection : IAsyncDisposable
{
    public IConnection Connection { get; }
    public IChannel Channel { get; }

    public RabbitMqConnection(RabbitMqOptions options)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.Host,
            Port = options.Port,
            UserName = options.Username,
            Password = options.Password
            // Removido: DispatchConsumersAsync = true
        };

        Connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        Channel = Connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        await Channel.CloseAsync();
        await Connection.CloseAsync();
    }
}
