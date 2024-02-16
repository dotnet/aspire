// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a callback context for environment variables associated with a publisher.
/// </summary>
/// <param name="executionContext">The execution context for this invocation of the AppHost.</param>
/// <param name="environmentVariables">The environment variables associated with this execution.</param>
public class EnvironmentCallbackContext(DistributedApplicationExecutionContext executionContext, Dictionary<string, string>? environmentVariables = null)
{
    /// <summary>
    /// Obsolete. Use ExecutionContext instead. Will be removed in next preview.
    /// </summary>
    [Obsolete("Use ExecutionContext instead")]
    public string PublisherName => ExecutionContext.Operation == DistributedApplicationOperation.Publish ? "manifest" : "dcp";

    /// <summary>
    /// Gets the environment variables associated with the callback context.
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = environmentVariables ?? new();

    /// <summary>
    /// Gets the execution context associated with this invocation of the AppHost.
    /// </summary>
    public DistributedApplicationExecutionContext ExecutionContext { get; } = executionContext;
}
