// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Eventing;

/// <inheritdoc cref="IDistributedApplicationEventing" />
public class DistributedApplicationEventing : IDistributedApplicationEventing
{
    private readonly ConcurrentDictionary<Type, List<DistributedApplicationEventSubscription>> _eventSubscriptionListLookup = new();
    private readonly ConcurrentDictionary<DistributedApplicationEventSubscription, Type> _subscriptionEventTypeLookup = new();

    /// <inheritdoc cref="IDistributedApplicationEventing.PublishAsync{T}(T, CancellationToken)" />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Cancellation token")]
    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IDistributedApplicationEvent
    {
        return PublishAsync(@event, EventDispatchBehavior.BlockingSequential, cancellationToken);
    }

    /// <inheritdoc cref="IDistributedApplicationEventing.PublishAsync{T}(T, CancellationToken)" />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Cancellation token")]
    public async Task PublishAsync<T>(T @event, EventDispatchBehavior dispatchBehavior, CancellationToken cancellationToken = default) where T : IDistributedApplicationEvent
    {
        if (_eventSubscriptionListLookup.TryGetValue(typeof(T), out var subscriptions))
        {
            if (dispatchBehavior == EventDispatchBehavior.BlockingConcurrent || dispatchBehavior == EventDispatchBehavior.NonBlockingConcurrent)
            {
                var pendingSubscriptionCallbacks = new List<Task>(subscriptions.Count);
                foreach (var subscription in subscriptions.ToArray())
                {
                    var pendingSubscriptionCallback = subscription.Callback(@event, cancellationToken);
                    pendingSubscriptionCallbacks.Add(pendingSubscriptionCallback);
                }

                if (dispatchBehavior == EventDispatchBehavior.NonBlockingConcurrent)
                {
                    // Non-blocking concurrent.
                    _ = Task.Run(async () =>
                    {
                        await Task.WhenAll(pendingSubscriptionCallbacks).ConfigureAwait(false);
                    }, default);
                }
                else
                {
                    // Blocking concurrent.
                    await Task.WhenAll(pendingSubscriptionCallbacks).ConfigureAwait(false);
                }
            }
            else
            {
                if (dispatchBehavior == EventDispatchBehavior.NonBlockingSequential)
                {
                    // Non-blocking sequential.
                    _ = Task.Run(async () =>
                    {
                        foreach (var subscription in subscriptions.ToArray())
                        {
                            await subscription.Callback(@event, cancellationToken).ConfigureAwait(false);
                        }
                    }, default);
                }
                else
                {
                    // Blocking sequential.
                    foreach (var subscription in subscriptions.ToArray())
                    {
                        await subscription.Callback(@event, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }

    /// <inheritdoc cref="IDistributedApplicationEventing.Subscribe{T}(Func{T, CancellationToken, Task})" />
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
            if (!_eventSubscriptionListLookup.TryAdd(typeof(T), new List<DistributedApplicationEventSubscription> { subscription }))
            {
                // This code only executes if we try get the subscription list and it fails, and then it is subsequently
                // added by another thread. In this case we just add our subscription. We don't invert this logic because
                // we don't want to allocate a list each time someone wants to subscribe to an event.
                _eventSubscriptionListLookup[typeof(T)].Add(subscription);
            }
        }

        _subscriptionEventTypeLookup[subscription] = typeof(T);

        return subscription;
    }

    /// <inheritdoc cref="IDistributedApplicationEventing.Subscribe{T}(Func{T, CancellationToken, Task})" />
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
    public void Unsubscribe(DistributedApplicationEventSubscription subscription)
    {
        if (_subscriptionEventTypeLookup.TryGetValue(subscription, out var eventType))
        {
            if (_eventSubscriptionListLookup.TryGetValue(eventType, out var subscriptions))
            {
                subscriptions.Remove(subscription);
                _subscriptionEventTypeLookup.Remove(subscription, out _);
            }
        }
    }
}
