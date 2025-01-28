// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dapr;

/// <summary>
/// Represents a Dapr component resource.
/// </summary>
[Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
public sealed class DaprComponentResource : Resource, IDaprComponentResource
{
    /// <summary>
    /// Initializes a new instance of <see cref="DaprComponentResource"/>.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <param name="type">The Dapr component type. This may be a generic "state" or "pubsub" if Aspire should choose an appropriate type when running or deploying.</param>
    public DaprComponentResource(string name, string type) : base(name)
    {
        this.Type = type;
    }

    /// <inheritdoc/>
    public string Type { get; }

    /// <inheritdoc/>
    public DaprComponentOptions? Options { get; init; }
}
