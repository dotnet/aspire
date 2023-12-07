// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public interface IDashboardViewModelService
{
    string ApplicationName { get; }

    ViewModelMonitor GetResources();
}

public record ViewModelMonitor(
    List<ResourceViewModel> Snapshot,
    IAsyncEnumerable<ResourceChange> Watch);
