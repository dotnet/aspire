// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Fast.Components.FluentUI;

namespace Aspire.Dashboard.Components.Dialogs;
public partial class FilterDialog
{

    [CascadingParameter]
    public FluentDialog? Dialog { get; set; }

    [Parameter]
    public FilterDialogViewModel Content { get; set; } = default!;

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; set; }

    private string Parameter { get; set; } = default!;
    private FilterCondition Condition { get; set; }
    private string Value { get; set; } = default!;

    protected override void OnInitialized()
    {
        if (Content.Filter is { } logFilter)
        {
            Parameter = logFilter.Field;
            Condition = logFilter.Condition;
            Value = logFilter.Value;
        }
        else
        {
            Parameter = "Message";
            Condition = FilterCondition.Contains;
            Value = "";
        }
    }

    public List<string> Parameters
    {
        get
        {
            var result = new List<string> { "Message", "Application", "TraceId", "SpanId", "ParentId", "OriginalFormat" };
            result.AddRange(Content.LogPropertyKeys);
            return result;
        }
    }

    public Dictionary<FilterCondition, string> Conditions
    {
        get
        {
            var result = new Dictionary<FilterCondition, string>();
            foreach (var c in Enum.GetValues<FilterCondition>())
            {
                result.Add(c, LogFilter.ConditionToString(c));
            }
            return result;
        }
    }

    private void Cancel()
    {
        Dialog!.CancelAsync();
    }

    private void Delete()
    {
        Dialog!.CloseAsync(DialogResult.Ok(new FilterDialogResult() { Filter = Content.Filter, Delete = true }));
    }

    private void Apply()
    {
        if (Content.Filter is { } logFilter)
        {
            logFilter.Field = Parameter;
            logFilter.Condition = Condition;
            logFilter.Value = Value;

            Dialog!.CloseAsync(DialogResult.Ok(new FilterDialogResult() { Filter = logFilter, Delete = false }));
        }
        else
        {
            var filter = new LogFilter
            {
                Field = Parameter,
                Condition = Condition,
                Value = Value
            };
            Dialog!.CloseAsync(DialogResult.Ok(new FilterDialogResult() { Filter = filter, Add = true }));
        }
    }
}