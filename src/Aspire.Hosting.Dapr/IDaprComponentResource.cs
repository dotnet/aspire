// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dapr;

/// <summary>
/// Represents a Dapr component resource.
/// </summary>
[Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
public interface IDaprComponentResource : IResource, IResourceWithWaitSupport
{
    /// <summary>
    /// Gets the type of the Dapr component.
    /// </summary>
    /// <remarks>
    /// This may be a generic "state" or "pubsub" if Aspire should choose an appropriate type when running or deploying.
    /// </remarks>
    string Type { get; }

    /// <summary>
    /// Gets options used to configure the component, if any.
    /// </summary>
    DaprComponentOptions? Options { get; }
}
