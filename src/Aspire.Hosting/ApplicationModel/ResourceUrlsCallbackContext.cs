// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// 
/// </summary>
public class ResourceUrlsCallbackContext(DistributedApplicationExecutionContext executionContext, IResource resource, List<ResourceUrlAnnotation>? urls = null, CancellationToken cancellationToken = default)
{
    /// <summary>
    /// Gets the resource this the URLs are associated with.
    /// </summary>
    public IResource Resource { get; } = resource;

    /// <summary>
    /// Gets the URLs associated with the callback context.
    /// </summary>
    public List<ResourceUrlAnnotation> Urls { get; } = urls ?? [];

    /// <summary>
    /// Gets the CancellationToken associated with the callback context.
    /// </summary>
    public CancellationToken CancellationToken { get; } = cancellationToken;

    /// <summary>
    /// An optional logger to use for logging.
    /// </summary>
    public ILogger Logger { get; set; } = NullLogger.Instance;

    /// <summary>
    /// Gets the execution context associated with this invocation of the AppHost.
    /// </summary>
    public DistributedApplicationExecutionContext ExecutionContext { get; } = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
}
