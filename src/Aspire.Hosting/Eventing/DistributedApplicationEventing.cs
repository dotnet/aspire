using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Eventing;

/// <inheritdoc cref="IDistributedApplicationEventing" />
[Experimental("ASPIREEVENTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public class DistributedApplicationEventing : IDistributedApplicationEventing
{
    private readonly Dictionary<Type, List<DistributedApplicationEventSubscription>> _eventSubscriptionListLookup = new();
    private readonly Dictionary<DistributedApplicationEventSubscription, Type> _subscriptionEventTypeLookup = new();

    /// <inheritdoc cref="IDistributedApplicationEventing.PublishAsync{T}(T, CancellationToken)" />
    [Experimental("ASPIREEVENTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken) where T : IDistributedApplicationEvent
    {
        if (_eventSubscriptionListLookup.TryGetValue(typeof(T), out var subscriptions))
        {
            // Taking a snapshot of the subscription list to avoid any concurrency issues
            // whilst we iterate over the subscriptions. Subscribers could result in the
            // subscriptions being removed from the list.
            foreach (var subscription in subscriptions.ToArray())
            {
                await subscription.Callback(@event, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc cref="IDistributedApplicationEventing.Subscribe{T}(Func{T, CancellationToken, Task})" />
    [Experimental("ASPIREEVENTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public DistributedApplicationEventSubscription Subscribe<T>(Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationEvent
    {
        var subscription = new DistributedApplicationEventSubscription(async (@event, ct) =>
        {
            var typedEvent = (T)@event;
            await callback(typedEvent, ct).ConfigureAwait(false);
        });

        if (_eventSubscriptionListLookup.TryGetValue(typeof(T), out var subscriptions))
        {
            subscriptions.Add(subscription);
        }
        else
        {
            _eventSubscriptionListLookup[typeof(T)] = new List<DistributedApplicationEventSubscription> { subscription };
        }

        _subscriptionEventTypeLookup[subscription] = typeof(T);

        return subscription;
    }

    /// <inheritdoc cref="IDistributedApplicationEventing.Subscribe{T}(Func{T, CancellationToken, Task})" />
    [Experimental("ASPIREEVENTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public DistributedApplicationEventSubscription Subscribe<T>(IResource resource, Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationResourceEvent
    {
        var resourceFilteredCallback = async (T @event, CancellationToken cancellationToken) =>
        {
            if (@event.Resource == resource)
            {
                await callback(@event, cancellationToken).ConfigureAwait(false);
            }
        };

        return Subscribe(resourceFilteredCallback);
    }

    /// <inheritdoc cref="IDistributedApplicationEventing.Unsubscribe(DistributedApplicationEventSubscription)" />
    [Experimental("ASPIREEVENTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public void Unsubscribe(DistributedApplicationEventSubscription subscription)
    {
        if (_subscriptionEventTypeLookup.TryGetValue(subscription, out var eventType))
        {
            if (_eventSubscriptionListLookup.TryGetValue(eventType, out var subscriptions))
            {
                subscriptions.Remove(subscription);
                _subscriptionEventTypeLookup.Remove(subscription);
            }
        }
    }
}
