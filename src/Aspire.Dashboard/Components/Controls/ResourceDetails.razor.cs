// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class ResourceDetails
{
    [Parameter, EditorRequired]
    public required ResourceViewModel Resource { get; set; }

    [Parameter]
    public bool ShowSpecOnlyToggle { get; set; }

    [Inject]
    public required IStringLocalizer<Resources.Resources> Loc { get; init; }

    [Inject]
    public required ILogger<ResourceDetails> Logger { get; init; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    private bool IsSpecOnlyToggleDisabled => !Resource.Environment.All(i => !i.FromSpec) && !GetResourceValues().Any(v => v.KnownProperty == null);

    private bool _showAll;

    private IQueryable<EnvironmentVariableViewModel> FilteredItems =>
        Resource.Environment.Where(vm =>
            (_showAll || vm.FromSpec) &&
            (vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
            vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true)
        ).AsQueryable();

    private IQueryable<DisplayedEndpoint> FilteredEndpoints => GetEndpoints()
        .Where(v => v.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) || v.Text.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true)
        .AsQueryable();

    private IQueryable<SummaryValue> FilteredResourceValues => GetResourceValues()
        .Where(v => _showAll || v.KnownProperty != null)
        .Where(v => v.Key.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) || v.Tooltip.Contains(_filter, StringComparison.CurrentCultureIgnoreCase))
        .AsQueryable();

    private string _filter = "";
    private bool _areEnvironmentVariablesMasked = true;

    private readonly GridSort<EnvironmentVariableViewModel> _nameSort = GridSort<EnvironmentVariableViewModel>.ByAscending(vm => vm.Name);
    private readonly GridSort<EnvironmentVariableViewModel> _valueSort = GridSort<EnvironmentVariableViewModel>.ByAscending(vm => vm.Value);

    private List<KnownProperty> _resourceProperties = default!;
    private List<KnownProperty> _projectProperties = default!;
    private List<KnownProperty> _executableProperties = default!;
    private List<KnownProperty> _containerProperties = default!;

    protected override void OnInitialized()
    {
        // Known properties can't be static because they need the localizer for translations.
        _resourceProperties =
        [
            new KnownProperty(KnownProperties.Resource.DisplayName, Loc[Resources.Resources.ResourcesDetailsDisplayNameProperty]),
            new KnownProperty(KnownProperties.Resource.State, Loc[Resources.Resources.ResourcesDetailsStateProperty]),
            new KnownProperty(KnownProperties.Resource.CreateTime, Loc[Resources.Resources.ResourcesDetailsStartTimeProperty]),
            new KnownProperty(KnownProperties.Resource.ExitCode, Loc[Resources.Resources.ResourcesDetailsExitCodeProperty])
        ];
        _projectProperties =
        [
            .. _resourceProperties,
            new KnownProperty(KnownProperties.Project.Path, Loc[Resources.Resources.ResourcesDetailsProjectPathProperty]),
            new KnownProperty(KnownProperties.Executable.Pid, Loc[Resources.Resources.ResourcesDetailsExecutableProcessIdProperty]),
        ];
        _executableProperties =
        [
            .. _resourceProperties,
            new KnownProperty(KnownProperties.Executable.Path, Loc[Resources.Resources.ResourcesDetailsExecutablePathProperty]),
            new KnownProperty(KnownProperties.Executable.WorkDir, Loc[Resources.Resources.ResourcesDetailsExecutableWorkingDirectoryProperty]),
            new KnownProperty(KnownProperties.Executable.Args, Loc[Resources.Resources.ResourcesDetailsExecutableArgumentsProperty]),
            new KnownProperty(KnownProperties.Executable.Pid, Loc[Resources.Resources.ResourcesDetailsExecutableProcessIdProperty]),
        ];
        _containerProperties =
        [
            .. _resourceProperties,
            new KnownProperty(KnownProperties.Container.Image, Loc[Resources.Resources.ResourcesDetailsContainerImageProperty]),
            new KnownProperty(KnownProperties.Container.Id, Loc[Resources.Resources.ResourcesDetailsContainerIdProperty]),
            new KnownProperty(KnownProperties.Container.Command, Loc[Resources.Resources.ResourcesDetailsContainerCommandProperty]),
            new KnownProperty(KnownProperties.Container.Args, Loc[Resources.Resources.ResourcesDetailsContainerArgumentsProperty]),
            new KnownProperty(KnownProperties.Container.Ports, Loc[Resources.Resources.ResourcesDetailsContainerPortsProperty]),
        ];
    }

    protected override void OnParametersSet()
    {
        foreach (var vm in Resource.Environment.Where(vm => vm.IsValueMasked != _areEnvironmentVariablesMasked))
        {
            vm.IsValueMasked = _areEnvironmentVariablesMasked;
        }
    }

    private IEnumerable<DisplayedEndpoint> GetEndpoints()
    {
        return ResourceEndpointHelpers.GetEndpoints(Resource, includeInteralUrls: true);
    }

    private IEnumerable<SummaryValue> GetResourceValues()
    {
        var resolvedKnownProperties = Resource.ResourceType switch
        {
            KnownResourceTypes.Project => _projectProperties,
            KnownResourceTypes.Executable => _executableProperties,
            KnownResourceTypes.Container => _containerProperties,
            _ => _resourceProperties
        };

        // This is a left outer join for the SQL fans.
        // Return the resource properties, with an optional known property.
        // Order properties by the known property order. Unmatched properties are last.
        var values = Resource.Properties
            .Where(p => !p.Value.HasNullValue && !(p.Value.KindCase == Value.KindOneofCase.ListValue && p.Value.ListValue.Values.Count == 0))
            .GroupJoin(
                resolvedKnownProperties,
                p => p.Key,
                k => k.Key,
                (p, k) => new SummaryValue { Key = p.Key, Value = p.Value, KnownProperty = k.SingleOrDefault(), Tooltip = GetTooltip(p.Value) })
            .OrderBy(v => v.KnownProperty != null ? resolvedKnownProperties.IndexOf(v.KnownProperty) : int.MaxValue);

        return values;
    }

    private static string GetTooltip(Value value)
    {
        if (value.HasStringValue)
        {
            return value.StringValue;
        }
        else
        {
            return value.ToString();
        }
    }

    private static string GetDisplayedValue(BrowserTimeProvider timeProvider, SummaryValue summaryValue)
    {
        string value;
        if (summaryValue.Value.HasStringValue)
        {
            value = summaryValue.Value.StringValue;
        }
        else
        {
            // Complex values such as arrays and objects will be output as JSON.
            // Consider how complex values are rendered in the future.
            value = summaryValue.Value.ToString();
        }
        if (summaryValue.Key == KnownProperties.Container.Id)
        {
            // Container images have a short ID of 12 characters
            value = value.Substring(0, Math.Min(value.Length, 12));
        }
        else
        {
            // Dates are returned as ISO 8601 text.
            // Use try parse to check if a value matches ISO 8601 format. If there is a match then convert to a friendly format.
            if (DateTime.TryParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                value = FormatHelpers.FormatDateTime(timeProvider, date, cultureInfo: CultureInfo.CurrentCulture);
            }
        }

        return value;
    }

    private void ToggleMaskState()
    {
        if (Resource.Environment is var environment)
        {
            foreach (var vm in environment)
            {
                vm.IsValueMasked = _areEnvironmentVariablesMasked;
            }
        }
    }

    private void CheckAllMaskStates()
    {
        var foundMasked = false;
        var foundUnmasked = false;

        foreach (var vm in Resource.Environment)
        {
            foundMasked |= vm.IsValueMasked;
            foundUnmasked |= !vm.IsValueMasked;
        }

        _areEnvironmentVariablesMasked = foundMasked switch
        {
            false when foundUnmasked => false,
            true when !foundUnmasked => true,
            _ => _areEnvironmentVariablesMasked
        };
    }

    private sealed class SummaryValue
    {
        public required string Key { get; init; }
        public required Value Value { get; init; }
        public required string Tooltip { get; init; }
        public KnownProperty? KnownProperty { get; set; }
    }

    private sealed record KnownProperty(string Key, string DisplayName);
}
