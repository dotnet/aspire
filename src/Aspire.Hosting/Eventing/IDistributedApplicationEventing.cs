// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Eventing;

/// <summary>
/// Supports publishing and subscribing to events which are executed during the AppHost lifecycle.
/// </summary>
public interface IDistributedApplicationEventing
{
    /// <summary>
    /// Subscribes a callback to a specific event type within the AppHost.
    /// </summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="callback">A callback to handle the event.</param>
    /// <returns>A subscription instance which can be used to unsubscribe </returns>
    DistributedApplicationEventSubscription Subscribe<T>(Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationEvent;

    /// <summary>
    /// Subscribes a callback to a specific event type 
    /// </summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="resource">The resource instance associated with the event.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <returns>A subscription instance which can be used to unsubscribe.</returns>
    DistributedApplicationEventSubscription Subscribe<T>(IResource resource, Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationResourceEvent;

    /// <summary>
    /// Unsubscribe from an event.
    /// </summary>
    /// <param name="subscription">The specific subscription to unsubscribe.</param>
    void Unsubscribe(DistributedApplicationEventSubscription subscription);

    /// <summary>
    /// Attempts to subscribe a callback to a specific event type within the AppHost only if a subscription with the same key doesn't already exist.
    /// </summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="key">A unique key to identify this subscription.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <param name="subscription">When this method returns true, contains the subscription instance which can be used to unsubscribe; otherwise, null.</param>
    /// <returns>true if the subscription was added; false if a subscription with the same key already exists.</returns>
    bool TrySubscribeOnce<T>(object key, Func<T, CancellationToken, Task> callback, [NotNullWhen(true)] out DistributedApplicationEventSubscription? subscription) where T : IDistributedApplicationEvent;

    /// <summary>
    /// Attempts to subscribe a callback to a specific event type within the AppHost only if a subscription with the same key doesn't already exist.
    /// Uses the IDistributedApplicationEventing instance as the key.
    /// </summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="callback">A callback to handle the event.</param>
    /// <param name="subscription">When this method returns true, contains the subscription instance which can be used to unsubscribe; otherwise, null.</param>
    /// <returns>true if the subscription was added; false if a subscription with the same key already exists.</returns>
    bool TrySubscribeOnce<T>(Func<T, CancellationToken, Task> callback, [NotNullWhen(true)] out DistributedApplicationEventSubscription? subscription) where T : IDistributedApplicationEvent;

    /// <summary>
    /// Attempts to subscribe a callback to a specific event type for a specific resource only if a subscription with the same key doesn't already exist.
    /// </summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="resource">The resource instance associated with the event.</param>
    /// <param name="key">A unique key to identify this subscription.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <param name="subscription">When this method returns true, contains the subscription instance which can be used to unsubscribe; otherwise, null.</param>
    /// <returns>true if the subscription was added; false if a subscription with the same key already exists.</returns>
    bool TrySubscribeOnce<T>(IResource resource, object key, Func<T, CancellationToken, Task> callback, [NotNullWhen(true)] out DistributedApplicationEventSubscription? subscription) where T : IDistributedApplicationResourceEvent;

    /// <summary>
    /// Attempts to subscribe a callback to a specific event type for a specific resource only if a subscription with the same key doesn't already exist.
    /// Uses the IDistributedApplicationEventing instance as the key.
    /// </summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="resource">The resource instance associated with the event.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <param name="subscription">When this method returns true, contains the subscription instance which can be used to unsubscribe; otherwise, null.</param>
    /// <returns>true if the subscription was added; false if a subscription with the same key already exists.</returns>
    bool TrySubscribeOnce<T>(IResource resource, Func<T, CancellationToken, Task> callback, [NotNullWhen(true)] out DistributedApplicationEventSubscription? subscription) where T : IDistributedApplicationResourceEvent;

    /// <summary>
    /// Publishes an event to all subscribes of the specific event type.
    /// </summary>
    /// <typeparam name="T">The type of the event</typeparam>
    /// <param name="event">The event.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that can be awaited.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Cancellation token")]
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IDistributedApplicationEvent;

    /// <summary>
    /// Publishes an event to all subscribes of the specific event type.
    /// </summary>
    /// <typeparam name="T">The type of the event</typeparam>
    /// <param name="event">The event.</param>
    /// <param name="dispatchBehavior">The dispatch behavior for the event.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that can be awaited.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Cancellation token")]
    Task PublishAsync<T>(T @event, EventDispatchBehavior dispatchBehavior, CancellationToken cancellationToken = default) where T : IDistributedApplicationEvent;
}
