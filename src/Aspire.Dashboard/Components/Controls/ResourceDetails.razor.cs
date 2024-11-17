// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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

    private bool IsSpecOnlyToggleDisabled => !Resource.Environment.All(i => !i.FromSpec) && !GetResourceProperties(ordered: false).Any(static vm => vm.KnownProperty is null);

    // NOTE Excludes endpoints as they don't expose sensitive items (and enumerating endpoints is non-trivial)
    private IEnumerable<IPropertyGridItem> SensitiveGridItems => Resource.Environment.Cast<IPropertyGridItem>().Concat(Resource.Properties.Values).Where(static vm => vm.IsValueSensitive);

    private bool _showAll;
    private ResourceViewModel? _resource;
    private readonly HashSet<string> _unmaskedItemNames = new();

    internal IQueryable<EnvironmentVariableViewModel> FilteredEnvironmentVariables =>
        Resource.Environment
            .Where(vm => (_showAll || vm.FromSpec) && ((IPropertyGridItem)vm).MatchesFilter(_filter))
            .AsQueryable();

    internal IQueryable<DisplayedEndpoint> FilteredEndpoints =>
        GetEndpoints()
            .Where(vm => vm.MatchesFilter(_filter))
            .AsQueryable();

    internal IQueryable<VolumeViewModel> FilteredVolumes =>
        Resource.Volumes
            .Where(vm => vm.MatchesFilter(_filter))
            .AsQueryable();

    internal IQueryable<HealthReportViewModel> FilteredHealthReports =>
        Resource.HealthReports
            .Where(vm => vm.MatchesFilter(_filter))
            .AsQueryable();

    internal IQueryable<ResourcePropertyViewModel> FilteredResourceProperties =>
        GetResourceProperties(ordered: true)
            .Where(vm => (_showAll || vm.KnownProperty != null) && vm.MatchesFilter(_filter))
            .AsQueryable();

    private bool _isVolumesExpanded;
    private bool _isEnvironmentVariablesExpanded;
    private bool _isEndpointsExpanded;
    private bool _isHealthChecksExpanded;

    private string _filter = "";
    private bool? _isMaskAllChecked;

    private bool IsMaskAllChecked
    {
        get => _isMaskAllChecked ?? false;
        set { _isMaskAllChecked = value; }
    }

    private readonly GridSort<DisplayedEndpoint> _endpointValueSort = GridSort<DisplayedEndpoint>.ByAscending(vm => vm.Url ?? vm.Text);

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(Resource, _resource))
        {
            // Reset masking when the resource changes.
            if (!string.Equals(Resource.Name, _resource?.Name, StringComparisons.ResourceName))
            {
                _isMaskAllChecked = true;
                _unmaskedItemNames.Clear();
            }

            _resource = Resource;

            // Collapse details sections when they have no data.
            _isEndpointsExpanded = GetEndpoints().Any();
            _isEnvironmentVariablesExpanded = _resource.Environment.Any();
            _isVolumesExpanded = _resource.Volumes.Any();
            _isHealthChecksExpanded = _resource.HealthReports.Any() || _resource.HealthStatus is null; // null means we're waiting for health reports

            foreach (var item in SensitiveGridItems)
            {
                if (_isMaskAllChecked != null)
                {
                    item.IsValueMasked = _isMaskAllChecked.Value;
                }
                else if (_unmaskedItemNames.Count > 0)
                {
                    item.IsValueMasked = !_unmaskedItemNames.Contains(item.Name);
                }
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
        Debug.Assert(_isMaskAllChecked != null);

        _unmaskedItemNames.Clear();

        foreach (var vm in SensitiveGridItems)
        {
            vm.IsValueMasked = _isMaskAllChecked.Value;
        }
    }

    private void OnValueMaskedChanged(IPropertyGridItem vm)
    {
        // Check the "Mask All" checkbox if all sensitive values are masked.
        var valueMaskedValues = SensitiveGridItems.Select(i => i.IsValueMasked).Distinct().ToList();
        if (valueMaskedValues.Count == 1)
        {
            _isMaskAllChecked = valueMaskedValues[0];
            _unmaskedItemNames.Clear();
        }
        else
        {
            _isMaskAllChecked = null;

            if (vm.IsValueMasked)
            {
                _unmaskedItemNames.Remove(vm.Name);
            }
            else
            {
                _unmaskedItemNames.Add(vm.Name);
            }
        }
    }
}
