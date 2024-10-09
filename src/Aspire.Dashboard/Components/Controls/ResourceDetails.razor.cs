// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class ResourceDetails
{
    [Parameter, EditorRequired]
    public required ResourceViewModel Resource { get; set; }

    [Parameter]
    public required ConcurrentDictionary<string, ResourceViewModel> ResourceByName { get; set; }

    [Parameter]
    public bool ShowSpecOnlyToggle { get; set; }

    [Inject]
    public required ILogger<ResourceDetails> Logger { get; init; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    private bool IsSpecOnlyToggleDisabled => !Resource.Environment.All(i => !i.FromSpec) && !GetResourceProperties(ordered: false).Any(static vm => vm.KnownProperty is null);

    // NOTE Excludes endpoints as they don't expose sensitive items (and enumerating endpoints is non-trivial)
    private IEnumerable<IPropertyGridItem> SensitiveGridItems => Resource.Environment.Cast<IPropertyGridItem>().Concat(Resource.Properties.Values).Where(static vm => vm.IsValueSensitive);

    private bool _showAll;
    private ResourceViewModel? _resource;

    private IQueryable<EnvironmentVariableViewModel> FilteredEnvironmentVariables =>
        Resource.Environment
            .Where(vm => (_showAll || vm.FromSpec) && ((IPropertyGridItem)vm).MatchesFilter(_filter))
            .AsQueryable();

    private IQueryable<DisplayedEndpoint> FilteredEndpoints =>
        GetEndpoints()
            .Where(vm => vm.MatchesFilter(_filter))
            .AsQueryable();

    private IQueryable<ResourceDetailRelationship> FilteredRelationships =>
        GetRelationships()
            .Where(vm => vm.MatchesFilter(_filter))
            .AsQueryable();

    private IQueryable<ResourceDetailRelationship> FilteredBackRelationships =>
        GetBackRelationships()
            .Where(vm => vm.MatchesFilter(_filter))
            .AsQueryable();

    private IQueryable<VolumeViewModel> FilteredVolumes =>
        Resource.Volumes
            .Where(vm => vm.MatchesFilter(_filter))
            .AsQueryable();

    private IQueryable<HealthReportViewModel> FilteredHealthReports =>
        Resource.HealthReports
            .Where(vm => vm.MatchesFilter(_filter))
            .AsQueryable();

    private IQueryable<ResourcePropertyViewModel> FilteredResourceProperties =>
        GetResourceProperties(ordered: true)
            .Where(vm => (_showAll || vm.KnownProperty != null) && vm.MatchesFilter(_filter))
            .AsQueryable();

    private bool _isVolumesExpanded;
    private bool _isEnvironmentVariablesExpanded;
    private bool _isEndpointsExpanded;
    private bool _isHealthChecksExpanded;

    private string _filter = "";
    private bool _isMaskAllChecked = true;

    private readonly GridSort<DisplayedEndpoint> _endpointValueSort = GridSort<DisplayedEndpoint>.ByAscending(vm => vm.Url ?? vm.Text);

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(Resource, _resource))
        {
            _resource = Resource;

            // Collapse details sections when they have no data.
            _isEndpointsExpanded = GetEndpoints().Any();
            _isEnvironmentVariablesExpanded = _resource.Environment.Any();
            _isVolumesExpanded = _resource.Volumes.Any();
            _isHealthChecksExpanded = _resource.HealthReports.Any() || _resource.HealthStatus is null; // null means we're waiting for health reports

            foreach (var item in SensitiveGridItems)
            {
                item.IsValueMasked = _isMaskAllChecked;
            }
        }
    }

    private IEnumerable<ResourceDetailRelationship> GetRelationships()
    {
        if (ResourceByName == null)
        {
            return [];
        }

        var items = new List<ResourceDetailRelationship>();

        foreach (var relationship in Resource.Relationships)
        {
            var matches = ResourceByName.Values
                .Where(r => string.Equals(r.DisplayName, relationship.ResourceName, StringComparisons.ResourceName))
                .Where(r => r.KnownState != KnownResourceState.Hidden)
                .ToList();

            foreach (var match in matches)
            {
                items.Add(new()
                {
                    Resource = match,
                    ResourceName = ResourceViewModel.GetResourceName(match, ResourceByName),
                    Type = relationship.Type
                });
            }
        }

        return items;
    }

    private IEnumerable<ResourceDetailRelationship> GetBackRelationships()
    {
        if (ResourceByName == null)
        {
            return [];
        }

        var items = new List<ResourceDetailRelationship>();

        var otherResources = ResourceByName.Values
            .Where(r => r != Resource)
            .Where(r => r.KnownState != KnownResourceState.Hidden);

        foreach (var otherResource in otherResources)
        {
            foreach (var relationship in otherResource.Relationships)
            {
                if (string.Equals(relationship.ResourceName, Resource.DisplayName, StringComparisons.ResourceName))
                {
                    items.Add(new()
                    {
                        Resource = otherResource,
                        ResourceName = ResourceViewModel.GetResourceName(otherResource, ResourceByName),
                        Type = relationship.Type
                    });
                }
            }
        }

        return items.OrderBy(r => r.ResourceName);
    }

    private List<DisplayedEndpoint> GetEndpoints()
    {
        return ResourceEndpointHelpers.GetEndpoints(Resource, includeInternalUrls: true);
    }

    private IEnumerable<ResourcePropertyViewModel> GetResourceProperties(bool ordered)
    {
        var vms = Resource.Properties.Values
            .Where(vm => vm.Value is { HasNullValue: false } and not { KindCase: Value.KindOneofCase.ListValue, ListValue.Values.Count: 0 });

        return ordered
            ? vms.OrderBy(vm => vm.Priority).ThenBy(vm => vm.Name)
            : vms;
    }

    private void OnMaskAllCheckedChanged()
    {
        foreach (var vm in SensitiveGridItems)
        {
            vm.IsValueMasked = _isMaskAllChecked;
        }
    }

    private void OnValueMaskedChanged()
    {
        // Check the "Mask All" checkbox if all sensitive values are masked.

        foreach (var item in SensitiveGridItems)
        {
            if (!item.IsValueMasked)
            {
                _isMaskAllChecked = false;
                return;
            }
        }
        _isMaskAllChecked = true;
    }

    public Task OnViewRelationshipAsync(ResourceDetailRelationship relationship)
    {
        NavigationManager.NavigateTo(DashboardUrls.ResourcesUrl(resource: relationship.Resource.Name));
        return Task.CompletedTask;
    }
}

public sealed class ResourceDetailRelationship
{
    public required ResourceViewModel Resource { get; init; }
    public required string ResourceName { get; init; }
    public required string Type { get; set; }

    public bool MatchesFilter(string filter)
    {
        return Resource.DisplayName.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
            Type.Contains(filter, StringComparison.CurrentCultureIgnoreCase);
    }
}
