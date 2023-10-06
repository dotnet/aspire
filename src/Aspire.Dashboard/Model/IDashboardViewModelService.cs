// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public interface IDashboardViewModelService
{
    public Task<List<ContainerViewModel>> GetContainersAsync();
    public Task<List<ExecutableViewModel>> GetExecutablesAsync();
    public Task<List<ProjectViewModel>> GetProjectsAsync();
    public IAsyncEnumerable<ComponentChanged<ContainerViewModel>> WatchContainersAsync(IEnumerable<NamespacedName>? existingContainers = null, CancellationToken cancellationToken = default);
    public IAsyncEnumerable<ComponentChanged<ExecutableViewModel>> WatchExecutablesAsync(IEnumerable<NamespacedName>? existingExecutables = null, CancellationToken cancellationToken = default);
    public IAsyncEnumerable<ComponentChanged<ProjectViewModel>> WatchProjectsAsync(IEnumerable<NamespacedName>? existingProjects = null, CancellationToken cancellationToken = default);
}
