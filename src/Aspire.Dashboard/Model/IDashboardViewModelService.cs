// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public interface IDashboardViewModelService
{
    public string ApplicationName { get; }
    public ViewModelMonitor<ContainerViewModel> GetContainers();
    public ViewModelMonitor<ExecutableViewModel> GetExecutables();
    public ViewModelMonitor<ProjectViewModel> GetProjects();
    public ViewModelMonitor<ResourceViewModel> GetResources();
}

public record ViewModelMonitor<TViewModel>(List<TViewModel> Snapshot, IAsyncEnumerable<ResourceChanged<TViewModel>> Watch)
    where TViewModel : ResourceViewModel;
