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
    public required BrowserTimeProvider TimeProvider { get; init; }

    private IQueryable<LogEntryPropertyViewModel> FilteredItems =>
        _logEntryAttributes.Select(p => new LogEntryPropertyViewModel { Name = p.Key, Value = p.Value })
            .Where(ApplyFilter).AsQueryable();

    private IQueryable<LogEntryPropertyViewModel> FilteredExceptionItems =>
        _exceptionAttributes.Select(p => new LogEntryPropertyViewModel { Name = p.Key, Value = p.Value })
            .Where(ApplyFilter).AsQueryable();

    private IQueryable<LogEntryPropertyViewModel> FilteredContextItems =>
        _contextAttributes.Select(p => new LogEntryPropertyViewModel { Name = p.Key, Value = p.Value })
            .Where(ApplyFilter).AsQueryable();

    private IQueryable<LogEntryPropertyViewModel> FilteredResourceItems =>
        ViewModel.LogEntry.Application.AllProperties().Select(p => new LogEntryPropertyViewModel { Name = p.Key, Value = p.Value })
            .Where(ApplyFilter).AsQueryable();

    private string _filter = "";

    private readonly GridSort<LogEntryPropertyViewModel> _nameSort = GridSort<LogEntryPropertyViewModel>.ByAscending(vm => vm.Name);
    private readonly GridSort<LogEntryPropertyViewModel> _valueSort = GridSort<LogEntryPropertyViewModel>.ByAscending(vm => vm.Value);
    private List<KeyValuePair<string, string>> _logEntryAttributes = null!;
    private List<KeyValuePair<string, string>> _contextAttributes = null!;
    private List<KeyValuePair<string, string>> _exceptionAttributes = null!;

    protected override void OnParametersSet()
    {
        // Move some attributes to separate lists, e.g. exception attributes to their own list.
        // Remaining attributes are displayed along side the message.
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
        MoveAttributes(attributes, _exceptionAttributes, a => a.Key.StartsWith("exception.", StringComparison.OrdinalIgnoreCase));

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

    private bool ApplyFilter(LogEntryPropertyViewModel vm)
    {
        return vm.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) ||
            vm.Value?.Contains(_filter, StringComparison.CurrentCultureIgnoreCase) == true;
    }

    // Sometimes a parent ID is added and the value is 0000000000. Don't display unhelpful IDs.
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
