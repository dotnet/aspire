// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents container registry information for deployment targets.
/// </summary>
public interface IContainerRegistry
{
    /// <summary>
    /// Gets the name of the container registry.
    /// </summary>
    ReferenceExpression Name { get; }

    /// <summary>
    /// Gets the endpoint URL of the container registry.
    /// </summary>
    /// <remarks>
    /// An empty endpoint value indicates a local container registry where images are built and used locally
    /// without being pushed to a remote registry (e.g., Docker Compose scenarios).
    /// </remarks>
    ReferenceExpression Endpoint { get; }
}
