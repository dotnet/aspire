// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls;

public partial class StructuredLogDetails
{
    [Parameter, EditorRequired]
    public required StructureLogsDetailsViewModel ViewModel { get; set; }

    [Inject]
    public required BrowserTimeProvider TimeProvider { get; init; }

    internal IQueryable<TelemetryPropertyViewModel> FilteredItems =>
        _logEntryAttributes.Where(ApplyFilter).AsQueryable();

    internal IQueryable<TelemetryPropertyViewModel> FilteredExceptionItems =>
        _exceptionAttributes.Where(ApplyFilter).AsQueryable();

    internal IQueryable<TelemetryPropertyViewModel> FilteredContextItems =>
        _contextAttributes.Where(ApplyFilter).AsQueryable();

    internal IQueryable<TelemetryPropertyViewModel> FilteredResourceItems =>
        ViewModel.LogEntry.ApplicationView.AllProperties().Select(p => new TelemetryPropertyViewModel { Name = p.DisplayName, Key = p.Key, Value = p.Value })
            .Where(ApplyFilter).AsQueryable();

    private string _filter = "";

    private List<TelemetryPropertyViewModel> _logEntryAttributes = null!;
    private List<TelemetryPropertyViewModel> _contextAttributes = null!;
    private List<TelemetryPropertyViewModel> _exceptionAttributes = null!;

    protected override void OnParametersSet()
    {
        // Move some attributes to separate lists, e.g. exception attributes to their own list.
        // Remaining attributes are displayed along side the message.
        var attributes = ViewModel.LogEntry.Attributes
            .Select(a => new TelemetryPropertyViewModel { Name = a.Key, Key = $"unknown-{a.Key}", Value = a.Value })
            .ToList();

        _contextAttributes =
        [
            new TelemetryPropertyViewModel { Name ="Category", Key = KnownStructuredLogFields.CategoryField, Value = ViewModel.LogEntry.Scope.ScopeName }
        ];
        MoveAttributes(attributes, _contextAttributes, a => a.Name is "event.name" or "logrecord.event.id" or "logrecord.event.name");
        if (HasTelemetryBaggage(ViewModel.LogEntry.TraceId))
        {
            _contextAttributes.Add(new TelemetryPropertyViewModel { Name = "TraceId", Key = KnownStructuredLogFields.TraceIdField, Value = ViewModel.LogEntry.TraceId });
        }
        if (HasTelemetryBaggage(ViewModel.LogEntry.SpanId))
        {
            _contextAttributes.Add(new TelemetryPropertyViewModel { Name = "SpanId", Key = KnownStructuredLogFields.SpanIdField, Value = ViewModel.LogEntry.SpanId });
        }
        if (HasTelemetryBaggage(ViewModel.LogEntry.ParentId))
        {
            _contextAttributes.Add(new TelemetryPropertyViewModel { Name = "ParentId", Key = KnownStructuredLogFields.ParentIdField, Value = ViewModel.LogEntry.ParentId });
        }

        _exceptionAttributes = [];
        MoveAttributes(attributes, _exceptionAttributes, a => a.Name.StartsWith("exception.", StringComparison.OrdinalIgnoreCase));

        _logEntryAttributes =
        [
            new TelemetryPropertyViewModel { Name = "Level", Key = KnownStructuredLogFields.LevelField, Value = ViewModel.LogEntry.Severity.ToString() },
            new TelemetryPropertyViewModel { Name = "Message", Key = KnownStructuredLogFields.MessageField, Value = ViewModel.LogEntry.Message },
            .. attributes,
        ];
    }

    private static void MoveAttributes(List<TelemetryPropertyViewModel> source, List<TelemetryPropertyViewModel> desintation, Func<TelemetryPropertyViewModel, bool> predicate)
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

    private bool ApplyFilter(TelemetryPropertyViewModel vm)
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
