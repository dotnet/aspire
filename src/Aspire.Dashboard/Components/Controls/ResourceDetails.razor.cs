// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class ResourceDetails
{
    [Parameter, EditorRequired]
    public required ResourceViewModel Resource { get; set; }

    [Parameter]
    public bool ShowSpecOnlyToggle { get; set; }

    [Inject]
    public required ILogger<ResourceDetails> Logger { get; init; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    private bool IsSpecOnlyToggleDisabled => !Resource.Environment.All(i => !i.FromSpec) && !GetResourceProperties(ordered: false).Any(static vm => vm.KnownProperty is null);

    // NOTE Excludes endpoints as they don't expose sensitive items (and enumerating endpoints is non-trivial)
    private IEnumerable<IPropertyGridItem> SensitiveGridItems => Resource.Environment.Cast<IPropertyGridItem>().Concat(Resource.Properties.Values).Where(static vm => vm.IsValueSensitive);

    private bool _showAll;
    private ResourceViewModel? _resource;

    private IQueryable<EnvironmentVariableViewModel> FilteredEnvironmentVariables =>
        Resource.Environment
            .Where(vm => (_showAll || vm.FromSpec) && vm.MatchesFilter(_filter))
            .AsQueryable();

    private IQueryable<DisplayedEndpoint> FilteredEndpoints =>
        GetEndpoints()
            .Where(vm => vm.MatchesFilter(_filter))
            .AsQueryable();

    private IQueryable<VolumeViewModel> FilteredVolumes =>
        Resource.Volumes.Where(vm =>
            vm.Source?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true ||
            vm.Target?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true
        ).AsQueryable();

    private IQueryable<ResourcePropertyViewModel> FilteredResourceProperties =>
        GetResourceProperties(ordered: true)
            .Where(vm => (_showAll || vm.KnownProperty != null) && vm.MatchesFilter(_filter))
            .AsQueryable();

    private bool _isVolumesExpanded;
    private bool _isEnvironmentVariablesExpanded;
    private bool _isEndpointsExpanded;

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

            foreach (var item in SensitiveGridItems)
            {
                item.IsValueMasked = _isMaskAllChecked;
            }
        }
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
}
