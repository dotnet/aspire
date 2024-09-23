// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Model.Otlp;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class FilterDialog
{
    private List<SelectViewModel<FilterCondition>> _filterConditions = null!;

    private SelectViewModel<FilterCondition> CreateFilterSelectViewModel(FilterCondition condition) =>
        new SelectViewModel<FilterCondition> { Id = condition, Name = TelemetryFilter.ConditionToString(condition, FilterLoc) };

    [CascadingParameter]
    public FluentDialog? Dialog { get; set; }

    [Parameter]
    public FilterDialogViewModel Content { get; set; } = default!;

    [Inject]
    public required TelemetryRepository TelemetryRepository { get; init; }

    private LogDialogFormModel _formModel = default!;
    private List<SelectViewModel<string>> _parameters = default!;

    public EditContext EditContext { get; private set; } = default!;

    protected override void OnInitialized()
    {
        _filterConditions =
        [
            CreateFilterSelectViewModel(FilterCondition.Equals),
            CreateFilterSelectViewModel(FilterCondition.Contains),
            CreateFilterSelectViewModel(FilterCondition.NotEqual),
            CreateFilterSelectViewModel(FilterCondition.NotContains)
        ];

        _formModel = new LogDialogFormModel();
        EditContext = new EditContext(_formModel);
    }

    protected override void OnParametersSet()
    {
        var knownFields = Content.KnownKeys.Select(p => new SelectViewModel<string> { Id = p, Name = TelemetryFilter.ResolveFieldName(p) }).ToList();
        var customFields = Content.PropertyKeys.Select(p => new SelectViewModel<string> { Id = p, Name = TelemetryFilter.ResolveFieldName(p) }).ToList();

        if (customFields.Count > 0)
        {
            _parameters =
            [
                .. knownFields,
                new SelectViewModel<string> { Id = null, Name = "-" },
                .. customFields
            ];
        }
        else
        {
            _parameters = knownFields;
        }

        if (Content.Filter is { } logFilter)
        {
            _formModel.Parameter = _parameters.SingleOrDefault(c => c.Id == logFilter.Field);
            _formModel.Condition = _filterConditions.Single(c => c.Id == logFilter.Condition);
            _formModel.Value = logFilter.Value;
        }
        else
        {
            _formModel.Parameter = _parameters.FirstOrDefault();
            _formModel.Condition = _filterConditions.Single(c => c.Id == FilterCondition.Contains);
            _formModel.Value = "";
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
            logFilter.Field = _formModel.Parameter!.Id!;
            logFilter.Condition = _formModel.Condition!.Id;
            logFilter.Value = _formModel.Value!;

            Dialog!.CloseAsync(DialogResult.Ok(new FilterDialogResult() { Filter = logFilter, Delete = false }));
        }
        else
        {
            var filter = new TelemetryFilter
            {
                Field = _formModel.Parameter!.Id!,
                Condition = _formModel.Condition!.Id,
                Value = _formModel.Value!
            };

            Dialog!.CloseAsync(DialogResult.Ok(new FilterDialogResult() { Filter = filter, Add = true }));
        }
    }
}
