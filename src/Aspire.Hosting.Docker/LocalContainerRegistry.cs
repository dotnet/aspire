// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Represents a local container registry (used for Docker Compose scenarios where images are built and used locally).
/// </summary>
internal sealed class LocalContainerRegistry : IContainerRegistry
{
    /// <summary>
    /// Gets a singleton instance of <see cref="LocalContainerRegistry"/>.
    /// </summary>
    public static LocalContainerRegistry Instance { get; } = new();

    private LocalContainerRegistry()
    {
    }

    /// <inheritdoc/>
    public ReferenceExpression Name => ReferenceExpression.Create($"local");

    /// <inheritdoc/>
    public ReferenceExpression Endpoint => ReferenceExpression.Create($"");

    /// <inheritdoc/>
    public ReferenceExpression? Repository => null;
}
