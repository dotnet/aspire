// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

/// <summary>
/// Provides data about active resources to external components, such as the dashboard.
/// </summary>
public interface IResourceService
{
    string ApplicationName { get; }

    ViewModelMonitor GetResources();
}

public record ViewModelMonitor(
    List<ResourceViewModel> Snapshot,
    IAsyncEnumerable<ResourceChange> Watch);
