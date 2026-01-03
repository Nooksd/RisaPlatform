using Microsoft.Extensions.DependencyInjection;
using Shared.Kernel.Primitives;

namespace Shared.BuildingBlocks.Messaging.RabbitMq;

public static class RabbitMqServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMq(
        this IServiceCollection services,
        RabbitMqOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        return services;
    }

    public static IServiceCollection AddRabbitMqConsumer<TEvent, THandler>(
        this IServiceCollection services)
        where TEvent : class, IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        services.AddScoped<THandler>();
        services.AddHostedService<RabbitMqConsumer<TEvent, THandler>>();
        return services;
    }
}
