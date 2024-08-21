// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
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

    private bool IsSpecOnlyToggleDisabled => !Resource.Environment.All(i => !i.FromSpec) && !GetResourceProperties(ordered: false).Any(static vm => vm.KnownProperty is null);

    // NOTE Excludes endpoints as they don't expose sensitive items (and enumerating endpoints is non-trivial)
    private IEnumerable<IPropertyGridItem> SensitiveGridItems => Resource.Environment.Cast<IPropertyGridItem>().Concat(Resource.Properties.Values).Where(static vm => vm.IsValueSensitive);

    private bool _showAll;
    private ResourceViewModel? _resource;

    private IQueryable<EnvironmentVariableViewModel> FilteredEnvironmentVariables =>
        Resource.Environment.Where(vm =>
            (_showAll || vm.FromSpec) &&
            (vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
            vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true)
        ).AsQueryable();

    private IQueryable<DisplayedEndpoint> FilteredEndpoints => GetEndpoints()
        .Where(vm => vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) || vm.Text.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true)
        .AsQueryable();

    private IQueryable<VolumeViewModel> FilteredVolumes =>
        Resource.Volumes.Where(vm =>
            vm.Source?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true ||
            vm.Target?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true
        ).AsQueryable();

    private IQueryable<ResourcePropertyViewModel> FilteredResourceProperties => GetResourceProperties(ordered: true)
        .Where(vm => _showAll || vm.KnownProperty != null)
        .Where(vm => vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) || vm.ToolTip.Contains(_filter, StringComparison.CurrentCultureIgnoreCase))
        .AsQueryable();

    private string _filter = "";
    private bool _isMaskAllChecked = true;

    private readonly GridSort<DisplayedEndpoint> _endpointValueSort = GridSort<DisplayedEndpoint>.ByAscending(vm => vm.Url ?? vm.Text);

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
            new KnownProperty(KnownProperties.Resource.ExitCode, Loc[Resources.Resources.ResourcesDetailsExitCodeProperty]),
            new KnownProperty(KnownProperties.Resource.HealthState, Loc[Resources.Resources.ResourcesDetailsHealthStateProperty])
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
        if (!ReferenceEquals(Resource, _resource))
        {
            _resource = Resource;

            foreach (var item in SensitiveGridItems)
            {
                item.IsValueMasked = _isMaskAllChecked;
            }
        }
    }

    private IEnumerable<DisplayedEndpoint> GetEndpoints()
    {
        return ResourceEndpointHelpers.GetEndpoints(Resource, includeInternalUrls: true);
    }

    private IEnumerable<ResourcePropertyViewModel> GetResourceProperties(bool ordered)
    {
        PopulateMissingKnownProperties();

        var vms = Resource.Properties.Values
            .Where(vm => vm.Value is { HasNullValue: false } and not { KindCase: Value.KindOneofCase.ListValue, ListValue.Values.Count: 0 });

        return ordered
            ? vms.OrderBy(vm => vm.Priority).ThenBy(vm => vm.Name)
            : vms;

        void PopulateMissingKnownProperties()
        {
            List<KnownProperty>? knownProperties = null;

            // Lazily populate some data on the view models, that wasn't available when they were constructed.
            foreach ((var key, var vm) in Resource.Properties)
            {
                // A priority of -1 means the known property hasn't been looked up. Do that once per property, now.
                if (vm.Priority == -1)
                {
                    knownProperties ??= GetKnownPropertiesForSelectedResourceType();

                    var found = false;

                    for (var i = 0; i < knownProperties.Count; i++)
                    {
                        var kp = knownProperties[i];

                        if (kp.Key == key)
                        {
                            found = true;
                            vm.KnownProperty = kp;
                            vm.Priority = i;
                            break;
                        }
                    }

                    if (!found)
                    {
                        vm.Priority = int.MaxValue;
                    }
                }
            }

            List<KnownProperty> GetKnownPropertiesForSelectedResourceType()
            {
                return Resource.ResourceType switch
                {
                    KnownResourceTypes.Project => _projectProperties,
                    KnownResourceTypes.Executable => _executableProperties,
                    KnownResourceTypes.Container => _containerProperties,
                    _ => _resourceProperties
                };
            }
        }
    }

    private static RenderFragment GetContentAfterValue(DisplayedEndpoint vm)
    {
        if (vm.Url is null)
        {
            return static builder => { };
        }

        return builder =>
        {
            builder.OpenElement(0, "a");
            builder.AddAttribute(1, "href", vm.Url);
            builder.AddContent(2, vm.Text);
            builder.CloseElement();
        };
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
