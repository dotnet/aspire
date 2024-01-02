// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
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

    private bool IsSpecOnlyToggleDisabled => Resource.Environment.Any(i => i.FromSpec == false) is false;

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

    private IQueryable<SummaryValue> FilteredSummaryValues => GetSummaryValues()
        .Where(v => v.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) || v.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true)
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

    private IEnumerable<SummaryValue> GetSummaryValues()
    {
        yield return new SummaryValue { Name = "Name", Value = Resource.DisplayName };
        yield return new SummaryValue { Name = "State", Value = Resource.State, Type = SummaryValueType.State };
        yield return new SummaryValue { Name = "Start time", Value = Resource.CreationTimeStamp.ToString() };
        if (Resource is ProjectViewModel project)
        {
            yield return new SummaryValue { Name = "Project path", Value = project.ProjectPath };
            yield return new SummaryValue { Name = "Process ID", Value = project.ProcessId?.ToString(CultureInfo.InvariantCulture) };
        }
        else if (Resource is ExecutableViewModel executable)
        {
            yield return new SummaryValue { Name = "Executable path", Value = executable.ExecutablePath };
            yield return new SummaryValue { Name = "Executable arguments", Value = (executable.Arguments is { } args) ? string.Join(" ", args) : null };
            yield return new SummaryValue { Name = "Working directory", Value = executable.WorkingDirectory };
            yield return new SummaryValue { Name = "Process ID", Value = executable.ProcessId?.ToString(CultureInfo.InvariantCulture) };
        }
        else if (Resource is ContainerViewModel container)
        {
            yield return new SummaryValue { Name = "Image", Value = container.Image };
            yield return new SummaryValue { Name = "Container ID", Value = container.ContainerId };
            yield return new SummaryValue { Name = "Ports", Value = string.Join(", ", container.Ports) };
            if (container.Command is { } command)
            {
                yield return new SummaryValue { Name = "Command", Value = command };
            }
            if (container.Args is { Length: > 0 } args)
            {
                yield return new SummaryValue { Name = "Arguments", Value = string.Join(" ", args) };
            }
        }
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
        public required string Name { get; init; }
        public string? Value { get; init; }
        public SummaryValueType Type { get; set; }
    }

    private enum SummaryValueType
    {
        Default,
        State
    }
}
