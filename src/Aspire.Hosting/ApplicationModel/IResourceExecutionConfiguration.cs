// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Configuration (arguments and environment variables) to apply to a specific resource.
/// </summary>
public interface IResourceExecutionConfiguration
{
    /// <summary>
    /// Gets the arguments to apply to the resource.
    /// </summary>
    IReadOnlyList<(string Value, bool IsSensitive)> Arguments { get; }

    /// <summary>
    /// Gets the environment variables to apply to the resource.
    /// </summary>
    IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

    /// <summary>
    /// Gets additional configuration data associated with the resource configuration.
    /// </summary>
    IReadOnlySet<IResourceExecutionConfigurationData> AdditionalConfigurationData { get; }

    /// <summary>
    /// Gets the exception that occurred while gathering the resource configuration, if any.
    /// If multiple exceptions occurred, they are aggregated into an AggregateException.
    /// </summary>
    Exception? Exception { get; }
}