// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public interface IDashboardViewModelService
{
    public string ApplicationName { get; }
    public Task<List<ContainerViewModel>> GetContainersAsync(CancellationToken cancellationToken = default);
    public Task<List<ExecutableViewModel>> GetExecutablesAsync(CancellationToken cancellationToken = default);
    public Task<List<ProjectViewModel>> GetProjectsAsync(CancellationToken cancellationToken = default);
    public IAsyncEnumerable<ResourceChanged<ContainerViewModel>> WatchContainersAsync(IEnumerable<NamespacedName>? existingContainers = null, CancellationToken cancellationToken = default);
    public IAsyncEnumerable<ResourceChanged<ExecutableViewModel>> WatchExecutablesAsync(IEnumerable<NamespacedName>? existingExecutables = null, CancellationToken cancellationToken = default);
    public IAsyncEnumerable<ResourceChanged<ProjectViewModel>> WatchProjectsAsync(IEnumerable<NamespacedName>? existingProjects = null, CancellationToken cancellationToken = default);
}
