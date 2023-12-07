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
    ResourceSubscription Subscribe();
}

public sealed record ResourceSubscription(
    List<ResourceViewModel> Snapshot,
    IAsyncEnumerable<ResourceChange> Subscription);
