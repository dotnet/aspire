// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Model.Otlp;
using Aspire.DashboardService.Proto.V1;

namespace Aspire.Dashboard.Model;

public sealed class InputViewModel
{
    public InteractionInput Input { get; private set; } = default!;

    public InputViewModel(InteractionInput input)
    {
        SetInput(input);
    }

    public void SetInput(InteractionInput input)
    {
        Input = input;
        if (input.InputType == InputType.Select && input.Options != null)
        {
            var optionsVM = input.Options
                .Select(option => new SelectViewModel<string> { Id = option.Key, Name = option.Value, })
                .ToList();

            SelectOptions.Clear();
            SelectOptions.AddRange(optionsVM);
        }
    }

    public List<SelectViewModel<string>> SelectOptions { get; } = [];

    public string? Value
    {
        get => Input.Value;
        set => Input.Value = value;
    }

    // Used when binding to FluentCheckbox.
    public bool IsChecked
    {
        get => bool.TryParse(Input.Value, out var result) && result;
        set => Input.Value = value ? "true" : "false";
    }

    // Used when binding to FluentNumberField.
    public int? NumberValue
    {
        get => int.TryParse(Input.Value, CultureInfo.InvariantCulture, out var result) ? result : null;
        set => Input.Value = value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
