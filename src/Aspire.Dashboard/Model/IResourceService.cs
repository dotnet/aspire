// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

/// <summary>
/// Provides data about active resources to external components, such as the dashboard.
/// </summary>
public interface IResourceService
{
    string ApplicationName { get; }

    /// <summary>
    /// Gets the current set of resources and a stream of updates.
    /// </summary>
    /// <remarks>
    /// The returned subscription will not complete on its own.
    /// Callers are required to manage the lifetime of the subscription,
    /// using cancellation during enumeration.
    /// </remarks>
    ResourceSubscription SubscribeResources();
}

public sealed record ResourceSubscription(
    List<ResourceViewModel> Snapshot,
    IAsyncEnumerable<ResourceChange> Subscription);
