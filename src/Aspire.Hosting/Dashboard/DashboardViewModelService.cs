// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NamespacedName = Aspire.Dashboard.Model.NamespacedName;

namespace Aspire.Hosting.Dashboard;

internal sealed partial class DashboardViewModelService : IDashboardViewModelService, IAsyncDisposable
{
    private const string AppHostSuffix = ".AppHost";

    private readonly DistributedApplicationModel _applicationModel;
    private readonly string _applicationName;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly ViewModelCache<Container, ContainerViewModel> _containerViewModelCache;
    private readonly ViewModelCache<Executable, ExecutableViewModel> _executableViewModelCache;
    private readonly ViewModelCache<Executable, ProjectViewModel> _projectViewModelCache;

    public DashboardViewModelService(
        DistributedApplicationModel applicationModel, KubernetesService kubernetesService, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory)
    {
        _applicationModel = applicationModel;
        _applicationName = ComputeApplicationName(hostEnvironment.ApplicationName);
        _containerViewModelCache = new ContainerViewModelCache(
            kubernetesService,
            _applicationModel,
            loggerFactory,
            _cancellationTokenSource.Token);
        _executableViewModelCache = new ExecutableViewModelCache(
            kubernetesService,
            _applicationModel,
            loggerFactory,
            _cancellationTokenSource.Token);
        _projectViewModelCache = new ProjectViewModelCache(
            kubernetesService,
            _applicationModel,
            loggerFactory,
            _cancellationTokenSource.Token);
    }

    public string ApplicationName => _applicationName;

    public ValueTask<List<ContainerViewModel>> GetContainersAsync() => _containerViewModelCache.GetResourcesAsync();
    public ValueTask<List<ExecutableViewModel>> GetExecutablesAsync() => _executableViewModelCache.GetResourcesAsync();
    public ValueTask<List<ProjectViewModel>> GetProjectsAsync() => _projectViewModelCache.GetResourcesAsync();

    public async IAsyncEnumerable<ResourceChanged<ContainerViewModel>> WatchContainersAsync(
        IEnumerable<NamespacedName>? existingContainers = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in _containerViewModelCache.WatchResourceAsync(existingContainers, cancellationToken))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<ResourceChanged<ExecutableViewModel>> WatchExecutablesAsync(
        IEnumerable<NamespacedName>? existingExecutables = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in _executableViewModelCache.WatchResourceAsync(existingExecutables, cancellationToken))
        {
            yield return item;
        }
    }

    public async IAsyncEnumerable<ResourceChanged<ProjectViewModel>> WatchProjectsAsync(
        IEnumerable<NamespacedName>? existingProjects = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in _projectViewModelCache.WatchResourceAsync(existingProjects, cancellationToken))
        {
            yield return item;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
    }

    private static string ComputeApplicationName(string applicationName)
    {
        if (applicationName.EndsWith(AppHostSuffix, StringComparison.OrdinalIgnoreCase))
        {
            applicationName = applicationName[..^AppHostSuffix.Length];
        }

        return applicationName;
    }
}
