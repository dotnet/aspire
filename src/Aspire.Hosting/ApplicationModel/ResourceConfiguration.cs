// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents the configuration (arguments and environment variables) to apply to a specific resource.
/// </summary>
internal class ResourceConfiguration : IResourceConfiguration
{
    /// <inheritdoc/>
    public required IReadOnlyList<(string Value, bool IsSensitive)> Arguments { get; init; }

    /// <inheritdoc/>
    public required IReadOnlyDictionary<string, string> EnvironmentVariables { get; init; }

    /// <inheritdoc/>
    public required IReadOnlySet<IResourceConfigurationMetadata> Metadata { get; init; }

    /// <inheritdoc/>
    public required Exception? Exception { get; init; }
}