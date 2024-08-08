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
    /// Unsubscribe from an event.
    /// </summary>
    /// <param name="subscription">The specific subscription to unsubscribe.</param>
    void Unsubscribe(DistributedApplicationEventSubscription subscription);

    /// <summary>
    /// Publishes an event to all subscribes of the specific event type.
    /// </summary>
    /// <typeparam name="T">The type of the event</typeparam>
    /// <param name="event">The event.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that can be awaited.</returns>
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken) where T : IDistributedApplicationEvent;
}
