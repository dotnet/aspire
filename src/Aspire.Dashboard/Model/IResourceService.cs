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

    /// <summary>
    /// Gets a stream of console log messages for the specified resource.
    /// Includes messages logged both before and after this method call.
    /// </summary>
    /// <remarks>
    /// <para>The returned sequence may end when the resource terminates.
    /// It is up to the implementation.</para>
    /// </remarks>
    /// <para>It is important that callers trigger <paramref name="cancellationToken"/>
    /// so that resources owned by the sequence and its consumers can be freed.</para>
    IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>? SubscribeConsoleLogs(string resourceName, CancellationToken cancellationToken);
}

public sealed record ResourceSubscription(
    List<ResourceViewModel> Snapshot,
    IAsyncEnumerable<ResourceChange> Subscription);
