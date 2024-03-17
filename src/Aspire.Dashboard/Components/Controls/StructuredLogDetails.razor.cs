// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class StructuredLogDetails
{
    [Parameter, EditorRequired]
    public required StructureLogsDetailsViewModel ViewModel { get; set; }

    [Inject]
    public required TimeProvider TimeProvider { get; init; }

    private IQueryable<LogEntryPropertyViewModel> FilteredItems =>
        _logEntryAttributes.Select(p => new LogEntryPropertyViewModel { Name = p.Key, Value = p.Value })
            .Where(vm =>
                (vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
                vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true)
        ).AsQueryable();

    private IQueryable<LogEntryPropertyViewModel> FilteredExceptionItems =>
        _exceptionAttributes.Select(p => new LogEntryPropertyViewModel { Name = p.Key, Value = p.Value })
            .Where(vm =>
                (vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
                vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true)
        ).AsQueryable();

    private IQueryable<LogEntryPropertyViewModel> FilteredContextItems =>
        _contextAttributes.Select(p => new LogEntryPropertyViewModel { Name = p.Key, Value = p.Value })
            .Where(vm =>
                (vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
                vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true)
        ).AsQueryable();

    private IQueryable<LogEntryPropertyViewModel> FilteredApplicationItems =>
        ViewModel.LogEntry.Application.AllProperties().Select(p => new LogEntryPropertyViewModel { Name = p.Key, Value = p.Value })
            .Where(vm =>
                vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
                vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true
        ).AsQueryable();

    private string _filter = "";

    private readonly GridSort<LogEntryPropertyViewModel> _nameSort = GridSort<LogEntryPropertyViewModel>.ByAscending(vm => vm.Name);
    private readonly GridSort<LogEntryPropertyViewModel> _valueSort = GridSort<LogEntryPropertyViewModel>.ByAscending(vm => vm.Value);

    private List<KeyValuePair<string, string>> _logEntryAttributes = null!;
    private List<KeyValuePair<string, string>> _contextAttributes = null!;
    private List<KeyValuePair<string, string>> _exceptionAttributes = null!;

    protected override void OnParametersSet()
    {
        var attributes = ViewModel.LogEntry.Attributes.ToList();

        _contextAttributes =
        [
            new KeyValuePair<string, string>("Category", ViewModel.LogEntry.Scope.ScopeName)
        ];
        MoveAttributes(attributes, _contextAttributes, a => a.Key is "event.name" or "logrecord.event.id" or "logrecord.event.name");
        if (HasTelemetryBaggage(ViewModel.LogEntry.TraceId))
        {
            _contextAttributes.Add(new KeyValuePair<string, string>("TraceId", ViewModel.LogEntry.TraceId));
        }
        if (HasTelemetryBaggage(ViewModel.LogEntry.SpanId))
        {
            _contextAttributes.Add(new KeyValuePair<string, string>("SpanId", ViewModel.LogEntry.SpanId));
        }
        if (HasTelemetryBaggage(ViewModel.LogEntry.ParentId))
        {
            _contextAttributes.Add(new KeyValuePair<string, string>("ParentId", ViewModel.LogEntry.ParentId));
        }

        _exceptionAttributes = [];
        MoveAttributes(attributes, _contextAttributes, a => a.Key.StartsWith("exception.", StringComparison.OrdinalIgnoreCase));

        _logEntryAttributes =
        [
            new KeyValuePair<string, string>("Level", ViewModel.LogEntry.Severity.ToString()),
            new KeyValuePair<string, string>("Message", ViewModel.LogEntry.Message),
            .. attributes,
        ];
    }

    private static void MoveAttributes(List<KeyValuePair<string, string>> source, List<KeyValuePair<string, string>> desintation, Func<KeyValuePair<string, string>, bool> predicate)
    {
        var insertStart = desintation.Count;
        for (var i = source.Count - 1; i >= 0; i--)
        {
            if (predicate(source[i]))
            {
                desintation.Insert(insertStart, source[i]);
                source.RemoveAt(i);
            }
        }
    }

    private static bool HasTelemetryBaggage(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != '0')
            {
                return true;
            }
        }

        return false;
    }
}
