// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a callback context for resource URLs.
/// </summary>
/// <param name="executionContext">The execution context.</param>
/// <param name="resource">The resource.</param>
/// <param name="urls">The URLs for the resource.</param>
/// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
public class ResourceUrlsCallbackContext(DistributedApplicationExecutionContext executionContext, IResource resource, List<ResourceUrlAnnotation>? urls = null, CancellationToken cancellationToken = default)
{
    /// <summary>
    /// Gets the resource this the URLs are associated with.
    /// </summary>
    public IResource Resource { get; } = resource;

    /// <summary>
    /// Gets an endpoint reference from <see cref="Resource"/> for the specified endpoint name.<br/>
    /// If <see cref="Resource"/> does not implement <see cref="IResourceWithEndpoints"/> then returns <c>null</c>.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public EndpointReference? GetEndpoint(string name) =>
        Resource switch
        {
            IResourceWithEndpoints resourceWithEndpoints => resourceWithEndpoints.GetEndpoint(name),
            _ => null
        };

    /// <summary>
    /// Gets the URLs associated with the callback context.
    /// </summary>
    public List<ResourceUrlAnnotation> Urls { get; } = urls ?? [];

    /// <summary>
    /// Gets the <see cref="CancellationToken"/> associated with the callback context.
    /// </summary>
    public CancellationToken CancellationToken { get; } = cancellationToken;

    /// <summary>
    /// A logger instance to use for logging.
    /// </summary>
    public ILogger Logger { get; set; } = NullLogger.Instance;

    /// <summary>
    /// Gets the execution context associated with this invocation of the AppHost.
    /// </summary>
    public DistributedApplicationExecutionContext ExecutionContext { get; } = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
}
