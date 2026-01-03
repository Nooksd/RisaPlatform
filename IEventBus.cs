public interface IEventBus
{
    Task PublishAsync<T>(T @event) where T : IIntegrationEvent;
    void Subscribe<T, TH>()
        where T : IIntegrationEvent
        where TH : IIntegrationEventHandler<T>;
}
