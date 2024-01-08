// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dapr;

/// <summary>
/// Represents a Dapr component resource.
/// </summary>
public sealed class DaprSidecarResource : Resource, IDaprSidecarResource
{
    /// <summary>
    /// Initializes a new instance of <see cref="DaprComponentResource"/>.
    /// </summary>
    /// <param name="name">The resource name.</param>
    public DaprSidecarResource(string name) : base(name)
    {
    }

    /// <inheritdoc/>
    public DaprSidecarOptions? Options { get; init; }
}
