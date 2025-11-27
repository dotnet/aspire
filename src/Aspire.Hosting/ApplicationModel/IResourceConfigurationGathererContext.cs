// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Resource configuration gatherer context.
/// </summary>
public interface IResourceConfigurationGathererContext
{
    /// <summary>
    /// The resource for which configuration is being gathered.
    /// </summary>
    IResource Resource { get; }

    /// <summary>
    /// The logger for the resource.
    /// </summary>
    ILogger ResourceLogger { get; }

    /// <summary>
    /// The execution context in which the resource is being configured.
    /// </summary>
    DistributedApplicationExecutionContext ExecutionContext { get; }

    /// <summary>
    /// Collection of resource command line arguments.
    /// </summary>
    List<object> Arguments { get; }

    /// <summary>
    /// Collection of resource environment variables.
    /// </summary>
    Dictionary<string, object> EnvironmentVariables { get; }

    /// <summary>
    /// Adds metadata associated with the resource configuration.
    /// </summary>
    /// <param name="metadata">The metadata to add.</param>
    void AddMetadata(IResourceConfigurationMetadata metadata);
}