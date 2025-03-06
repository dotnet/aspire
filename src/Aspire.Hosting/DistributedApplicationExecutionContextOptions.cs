// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Configuration options and references that need to be exposed to the <see cref="DistributedApplicationExecutionContext"/>.
/// </summary>
public class DistributedApplicationExecutionContextOptions(DistributedApplicationOperation operation, string? publisherName = null)
{
    /// <summary>
    /// The <see cref="IServiceProvider"/> for the AppHost.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; set; }

    /// <summary>
    /// The operation currently being performed by the AppHost.
    /// </summary>
    public DistributedApplicationOperation Operation { get; } = operation;

    /// <summary>
    /// The name of the publisher if running in pbublish mode.
    /// </summary>
    public string? PublisherName { get; } = publisherName;
}
