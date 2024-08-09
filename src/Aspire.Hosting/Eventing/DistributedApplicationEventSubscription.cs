using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Eventing;

/// <summary>
/// Represents a subscription to an event that is published during the lifecycle of the AppHost.
/// </summary>
public class DistributedApplicationEventSubscription(Func<IDistributedApplicationEvent, CancellationToken, Task> callback)
{
    /// <summary>
    /// The callback to be executed when the event is published.
    /// </summary>
    public Func<IDistributedApplicationEvent, CancellationToken, Task> Callback { get; } = callback;
}

/// <summary>
/// Represents a subscription to an event that is published during the lifecycle of the AppHost for a specific resource.
/// </summary>
public class DistributedApplicationResourceEventSubscription(IResource? resource, Func<IDistributedApplicationResourceEvent, CancellationToken, Task> callback)
    : DistributedApplicationEventSubscription((@event, cancellationToken) => callback((IDistributedApplicationResourceEvent)@event, cancellationToken))
{
    /// <summary>
    /// Resource associated with this subscription.
    /// </summary>
    public IResource? Resource { get; } = resource;
}
