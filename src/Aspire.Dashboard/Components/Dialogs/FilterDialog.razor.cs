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

    private FilterDialogFormModel _formModel = default!;
    private List<SelectViewModel<string>> _parameters = default!;
    private List<SelectViewModel<string>> _filteredValues = default!;
    private List<SelectViewModel<string>>? _allValues;

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

        _formModel = new FilterDialogFormModel();
        EditContext = new EditContext(_formModel);

        _filteredValues = [];
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

        UpdateParameterFieldValues();
        ValueChanged();
    }

    private void UpdateParameterFieldValues()
    {
        if (_formModel.Parameter?.Id is { } parameterName)
        {
            var fieldValues = Content.GetFieldValues(parameterName);
            _allValues = fieldValues
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key, StringComparers.OtlpFieldValue)
                .Select(kvp => new SelectViewModel<string> { Id = kvp.Key, Name = kvp.Key })
                .ToList();
        }
        else
        {
            _allValues = null;
        }
    }

    private async Task ParameterChangedAsync()
    {
        UpdateParameterFieldValues();

        _formModel.Value = "";
        StateHasChanged();

        // Clearing the selected value and the combo box items together wasn't correctly clearing the selected value.
        // This is hacky, but adding a delay between the two operations puts the combo box in the right state.
        await Task.Delay(100);
        ValueChanged();
    }

    // There is a bug in FluentUI that prevents the value changing immediately. Will be fixed in a future FluentUI update.
    // https://github.com/microsoft/fluentui-blazor/issues/2672
    private void ValueChanged()
    {
        if (_allValues != null)
        {
            IEnumerable<SelectViewModel<string>> newValues = _allValues;
            if (!string.IsNullOrEmpty(_formModel.Value))
            {
                newValues = newValues.Where(vm => vm.Name.Contains(_formModel.Value!));
            }

            // Limit to 1000 items to avoid the combo box have too many items and impacting UI perf.
            _filteredValues = newValues.Take(1000).ToList();
        }
        else
        {
            _filteredValues = [];
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
