// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public interface IDashboardViewModelService
{
    public Task<List<ResultWithSource<ContainerViewModel>>> GetContainersAsync();
    public Task<List<ResultWithSource<ExecutableViewModel>>> GetExecutablesAsync();
    public Task<List<ResultWithSource<ProjectViewModel>>> GetProjectsAsync();
    public IAsyncEnumerable<ComponentChanged<ContainerViewModel>> WatchContainersAsync(IEnumerable<object>? existingContainers = null, CancellationToken cancellationToken = default);
    public IAsyncEnumerable<ComponentChanged<ExecutableViewModel>> WatchExecutablesAsync(IEnumerable<object>? existingExecutables = null, CancellationToken cancellationToken = default);
    public IAsyncEnumerable<ComponentChanged<ProjectViewModel>> WatchProjectsAsync(IEnumerable<object>? existingProjects = null, CancellationToken cancellationToken = default);
}
