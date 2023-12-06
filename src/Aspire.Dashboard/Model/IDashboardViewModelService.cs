// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public interface IDashboardViewModelService
{
    string ApplicationName { get; }

    ViewModelMonitor<ResourceViewModel> GetResources();
}

public record ViewModelMonitor<TViewModel>(List<TViewModel> Snapshot, IAsyncEnumerable<ResourceChanged<TViewModel>> Watch)
    where TViewModel : ResourceViewModel;
