// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a callback context for environment variables associated with a publisher.
/// </summary>
/// <param name="executionContext">The execution context for this invocation of the AppHost.</param>
/// <param name="environmentVariables">The environment variables associated with this execution.</param>
/// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
public class EnvironmentCallbackContext(DistributedApplicationExecutionContext executionContext, Dictionary<string, object>? environmentVariables = null, CancellationToken cancellationToken = default)
{
    /// <summary>
    /// Gets the environment variables associated with the callback context.
    /// </summary>
    public Dictionary<string, object> EnvironmentVariables { get; } = environmentVariables ?? new();

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
