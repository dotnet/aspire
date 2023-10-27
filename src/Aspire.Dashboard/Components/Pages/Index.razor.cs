// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Fast.Components.FluentUI;

namespace Aspire.Dashboard.Components.Pages;

public partial class Index : ResourcesListBase<ProjectViewModel>
{
    private Subscription? _logsSubscription;

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    protected override ValueTask<List<ProjectViewModel>> GetResources(IDashboardViewModelService dashboardViewModelService)
        => dashboardViewModelService.GetProjectsAsync();

    protected override IAsyncEnumerable<ResourceChanged<ProjectViewModel>> WatchResources(
    IDashboardViewModelService dashboardViewModelService,
        IEnumerable<NamespacedName> initialList,
        CancellationToken cancellationToken)
        => dashboardViewModelService.WatchProjectsAsync(initialList, cancellationToken);

    protected override bool Filter(ProjectViewModel resource)
        => resource.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
        || resource.ProjectPath.Contains(filter, StringComparison.CurrentCultureIgnoreCase);

    private readonly GridSort<ProjectViewModel> _projectPathSort = GridSort<ProjectViewModel>.ByAscending(p => p.ProjectPath);

    private void ViewErrorStructuredLogs(ProjectViewModel project)
    {
        NavigationManager.NavigateTo($"/StructuredLogs/{project.Uid}?level=error");
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        _logsSubscription = TelemetryRepository.OnNewLogs(null, SubscriptionType.Listen, async () =>
        {
            await InvokeAsync(StateHasChanged);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _logsSubscription?.Dispose();
        }
    }
}
