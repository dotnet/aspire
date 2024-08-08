using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Published when a container is started.
/// </summary>
/// <remarks>
/// Not all resources that result in a container being launched derive from
/// <see cref="ContainerResource"/>. Some resoruces have a container annotation and result
/// in a container launching. This is the reason why the the <see cref="ContainerResourceStartedEvent.Resource"/>
/// property is of type <see cref="IResource"/> and not <see cref="ContainerResource"/>.
/// </remarks>
public class ContainerResourceStartedEvent(IResource resource) : IDistributedApplicationEvent
{
    /// <summary>
    /// The resource which is the subject of this event.
    /// </summary>
    public IResource Resource { get; } = resource;
}
