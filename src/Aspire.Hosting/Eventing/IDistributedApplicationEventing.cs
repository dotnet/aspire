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
    [Experimental("ASPIREEVENTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    DistributedApplicationEventSubscription Subscribe<T>(Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationEvent;

    /// <summary>
    /// Subscribes a callback to a specific event type 
    /// </summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="resource">The resource instance associated with the event.</param>
    /// <param name="callback">A callback to handle the event.</param>
    /// <returns>A subscription instance which can be used to unsubscribe.</returns>
    [Experimental("ASPIREEVENTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    DistributedApplicationEventSubscription Subscribe<T>(IResource resource, Func<T, CancellationToken, Task> callback) where T : IDistributedApplicationResourceEvent;

    /// <summary>
    /// Unsubscribe from an event.
    /// </summary>
    /// <param name="subscription">The specific subscription to unsubscribe.</param>
    [Experimental("ASPIREEVENTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    void Unsubscribe(DistributedApplicationEventSubscription subscription);

    /// <summary>
    /// Publishes an event to all subscribes of the specific event type.
    /// </summary>
    /// <typeparam name="T">The type of the event</typeparam>
    /// <param name="event">The event.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that can be awaited.</returns>
    [Experimental("ASPIREEVENTING001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IDistributedApplicationEvent;
}
