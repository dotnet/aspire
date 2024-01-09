// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class ResourceDetails
{
    [Parameter, EditorRequired]
    public required ResourceViewModel Resource { get; set; }

    [Parameter, EditorRequired]
    public Dictionary<OtlpApplication, int>? UnviewedErrorCounts { get; set; }

    [Parameter]
    public bool ShowSpecOnlyToggle { get; set; }

    private bool IsSpecOnlyToggleDisabled => !Resource.Environment.All(i => !i.FromSpec) && !GetResourceValues().Any(v => v.KnownProperty == null);

    private bool _showAll;

    private IQueryable<EnvironmentVariableViewModel> FilteredItems =>
        Resource.Environment.Where(vm =>
            (_showAll || vm.FromSpec) &&
            (vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
            vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true)
        ).AsQueryable();

    private IQueryable<Endpoint> FilteredEndpoints => GetEndpoints()
        .Where(v => v.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) || v.Address?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true)
        .AsQueryable();

    private IQueryable<SummaryValue> FilteredResourceValues => GetResourceValues()
        .Where(v => _showAll || v.KnownProperty != null)
        .Where(v => v.Key.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) || v.Tooltip.Contains(_filter, StringComparison.CurrentCultureIgnoreCase))
        .AsQueryable();

    private string _filter = "";
    private bool _defaultMasked = true;

    private readonly Icon _maskIcon = new Icons.Regular.Size16.EyeOff();
    private readonly Icon _unmaskIcon = new Icons.Regular.Size16.Eye();
    private readonly Icon _showSpecOnlyIcon = new Icons.Regular.Size16.DocumentHeader();
    private readonly Icon _showAllIcon = new Icons.Regular.Size16.DocumentOnePage();

    private readonly GridSort<EnvironmentVariableViewModel> _nameSort = GridSort<EnvironmentVariableViewModel>.ByAscending(vm => vm.Name);
    private readonly GridSort<EnvironmentVariableViewModel> _valueSort = GridSort<EnvironmentVariableViewModel>.ByAscending(vm => vm.Value);

    private IEnumerable<Endpoint> GetEndpoints()
    {
        foreach (var endpoint in Resource.Endpoints)
        {
            yield return new Endpoint { Name = "Endpoint Url", IsHttp = true, Address = endpoint.EndpointUrl };
            yield return new Endpoint { Name = "Proxy Url", IsHttp = true, Address = endpoint.ProxyUrl };
        }
        foreach (var service in Resource.Services)
        {
            yield return new Endpoint { Name = service.Name, IsHttp = false, Address = service.AddressAndPort };
        }
    }

    internal record KnownProperty(string Key, string DisplayName);

    private static readonly List<KnownProperty> s_resourceProperties =
    [
        new KnownProperty(KnownProperties.Resource.DisplayName, "Display name"),
        new KnownProperty(KnownProperties.Resource.State, "State"),
        new KnownProperty(KnownProperties.Resource.CreateTime, "Start time")
    ];
    private static readonly List<KnownProperty> s_projectProperties =
    [
        new KnownProperty(KnownProperties.Project.Path, "Project path"),
        new KnownProperty(KnownProperties.Executable.Pid, "Process ID"),
    ];
    private static readonly List<KnownProperty> s_executableProperties =
    [
        new KnownProperty(KnownProperties.Executable.Path, "Executable path"),
        new KnownProperty(KnownProperties.Executable.WorkDir, "Working directory"),
        new KnownProperty(KnownProperties.Executable.Args, "Executable arguments"),
        new KnownProperty(KnownProperties.Executable.Pid, "Process ID"),
    ];
    private static readonly List<KnownProperty> s_containerProperties =
    [
        new KnownProperty(KnownProperties.Container.Image, "Container image"),
        new KnownProperty(KnownProperties.Container.Id, "Container ID"),
        new KnownProperty(KnownProperties.Container.Command, "Container command"),
        new KnownProperty(KnownProperties.Container.Args, "Container arguments"),
        new KnownProperty(KnownProperties.Container.Ports, "Container ports"),
    ];

    private IEnumerable<SummaryValue> GetResourceValues()
    {
        var resolvedKnownProperties = Resource.ResourceType switch
        {
            KnownResourceTypes.Project => s_resourceProperties.Union(s_projectProperties).ToList(),
            KnownResourceTypes.Executable => s_resourceProperties.Union(s_executableProperties).ToList(),
            KnownResourceTypes.Container => s_resourceProperties.Union(s_containerProperties).ToList(),
            _ => s_resourceProperties
        };

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

    private static string GetDisplayedValue(SummaryValue summaryValue)
    {
        string value;
        if (summaryValue.Value.HasStringValue)
        {
            value = summaryValue.Value.StringValue;
        }
        else
        {
            value = summaryValue.Value.ToString();
        }
        if (summaryValue.Key == KnownProperties.Container.Id)
        {
            // Container images have a short ID of 12 characters
            value = value.Substring(0, Math.Min(value.Length, 12));
        }
        else
        {
            if (DateTime.TryParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                value = date.ToString(CultureInfo.CurrentCulture);
            }
        }

        return value;
    }

    private void ToggleMaskState()
    {
        _defaultMasked = !_defaultMasked;
        if (Resource.Environment is { } environment)
        {
            foreach (var vm in environment)
            {
                vm.IsValueMasked = _defaultMasked;
            }
        }
    }

    private void CheckAllMaskStates()
    {
        if (Resource.Environment is { } environment)
        {
            var foundMasked = false;
            var foundUnmasked = false;
            foreach (var vm in environment)
            {
                foundMasked |= vm.IsValueMasked;
                foundUnmasked |= !vm.IsValueMasked;
            }

            if (!foundMasked && foundUnmasked)
            {
                _defaultMasked = false;
            }
            else if (foundMasked && !foundUnmasked)
            {
                _defaultMasked = true;
            }
        }
    }

    private sealed class Endpoint
    {
        public bool IsHttp { get; init; }
        public required string Name { get; init; }
        public string? Address { get; init; }
    }

    private sealed class SummaryValue
    {
        public required string Key { get; init; }
        public required Value Value { get; init; }
        public required string Tooltip { get; init; }
        public KnownProperty? KnownProperty { get; set; }
    }
}
