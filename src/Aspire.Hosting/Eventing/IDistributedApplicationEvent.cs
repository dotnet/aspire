using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Eventing;

/// <summary>
/// Represents an event that is published during the lifecycle of the AppHost.
/// </summary>
public interface IDistributedApplicationEvent
{
}

/// <summary>
/// Represents an event that is published during the lifecycle of the AppHost for a specific resource.
/// </summary>
public interface IDistributedApplicationResourceEvent : IDistributedApplicationEvent
{
    /// <summary>
    /// Resource associated with this event.
    /// </summary>
    IResource Resource { get; }
}
