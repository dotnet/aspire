namespace Aspire.Hosting.Eventing;

internal class DistributedApplicationEventing : IDistributedApplicationEventing
{
    private readonly Dictionary<Type, List<DistributedApplicationEventSubscription>> _subscriptions = new();

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken) where T : IDistributedApplicationEvent
    {
        if (_subscriptions.TryGetValue(typeof(T), out var subscriptions))
        {
            foreach (var subscription in subscriptions)
            {
                await subscription.Callback(@event, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public DistributedApplicationEventSubscription Subscribe<T>(Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationEvent
    {
        var subscription = new DistributedApplicationEventSubscription(async (@event, ct) =>
        {
            var typedEvent = (T)@event;
            await callback(typedEvent, ct).ConfigureAwait(false);
        });

        if (_subscriptions.TryGetValue(typeof(T), out var subscriptions))
        {
            subscriptions.Add(subscription);
        }
        else
        {
            _subscriptions[typeof(T)] = new List<DistributedApplicationEventSubscription> { subscription };
        }

        return subscription;
    }

    public void Unsubscribe(DistributedApplicationEventSubscription subscription)
    {
    }
}
