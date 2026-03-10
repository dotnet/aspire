// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Stores cached unresolved environment variables and arguments gathered during
/// Phase 1 of container creation, so that callbacks are not re-invoked during Phase 2.
/// </summary>
internal sealed class CachedExecutionConfigurationAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Cached unresolved environment variable values (key → unresolved value object).
    /// </summary>
    public Dictionary<string, object> EnvironmentVariables { get; }

    /// <summary>
    /// Cached unresolved argument values.
    /// </summary>
    public List<object> Arguments { get; }

    /// <summary>
    /// The set of host resources with endpoints that this container depends on (for tunnel determination).
    /// </summary>
    public bool IsTunnelDependent { get; }

    public CachedExecutionConfigurationAnnotation(
        Dictionary<string, object> environmentVariables,
        List<object> arguments,
        bool isTunnelDependent)
    {
        EnvironmentVariables = environmentVariables;
        Arguments = arguments;
        IsTunnelDependent = isTunnelDependent;
    }
}
