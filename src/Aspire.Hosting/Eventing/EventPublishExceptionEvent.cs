// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Eventing;

/// <summary>
/// This event is raised when an exception occurs during event publishing.
/// </summary>
public sealed class EventPublishExceptionEvent : IDistributedApplicationEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventPublishExceptionEvent"/> class.
    /// </summary>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="eventType">The type of the event that was being published.</param>
    /// <param name="resource">The resource associated with the event, if it's a resource-specific event.</param>
    public EventPublishExceptionEvent(Exception exception, Type eventType, IResource? resource)
    {
        Exception = exception;
        EventType = eventType;
        Resource = resource;
    }

    /// <summary>
    /// The exception that was thrown.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// The type of the event that was being published.
    /// </summary>
    public Type EventType { get; }

    /// <summary>
    /// The resource associated with the event, if it's a resource-specific event.
    /// </summary>
    public IResource? Resource { get; }
}
