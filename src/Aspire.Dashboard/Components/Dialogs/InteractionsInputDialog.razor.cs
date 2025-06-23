// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.ResourceService.Proto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;

public class InputDialogInputViewModel
{
    public required InteractionInput Input { get; set; }

    public string? Value
    {
        get => Input.Value;
        set => Input.Value = value;
    }
    public int? NumberValue
    {
        get => Input.NumberValue;
        set => Input.NumberValue = value;
    }
    public bool IsChecked
    {
        get => Input.IsChecked;
        set => Input.IsChecked = value;
    }
}

public partial class InteractionsInputDialog
{
    [Parameter]
    public InteractionsInputsDialogViewModel Content { get; set; } = default!;

    [CascadingParameter]
    public FluentDialog Dialog { get; set; } = default!;

    private InteractionsInputsDialogViewModel? _content;
    private EditContext _editContext = default!;
    private ValidationMessageStore _validationMessages = default!;
    private List<InputDialogInputViewModel> _inputDialogInputViewModels = default!;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(Content);
        _validationMessages = new ValidationMessageStore(_editContext);

        _editContext.OnValidationRequested += (s, e) => ValidateModel();
        _editContext.OnFieldChanged += (s, e) => ValidateField(e.FieldIdentifier);
    }

    protected override void OnParametersSet()
    {
        if (_content != Content)
        {
            _content = Content;
            _inputDialogInputViewModels = Content.Inputs.Select(input => new InputDialogInputViewModel { Input = input }).ToList();

            Content.OnInteractionUpdated = async () =>
            {
                for (var i = 0; i < Content.Inputs.Count; i++)
                {
                    var inputModel = Content.Inputs[i];
                    var inputViewModel = _inputDialogInputViewModels[i];

                    inputViewModel.Input = inputModel;

                    var field = GetFieldIdentifier(inputViewModel);
                    foreach (var validationError in inputModel.ValidationErrors)
                    {
                        _validationMessages.Add(field, validationError);
                    }
                }

                await InvokeAsync(StateHasChanged);
            };
        }
    }

    private void ValidateModel()
    {
        _validationMessages.Clear();

        foreach (var inputModel in _inputDialogInputViewModels)
        {
            var field = GetFieldIdentifier(inputModel);
            if (IsMissingRequiredValue(inputModel))
            {
                _validationMessages.Add(field, $"{inputModel.Input.Label} is required.");
            }
        }

        _editContext.NotifyValidationStateChanged();
    }

    private void ValidateField(FieldIdentifier field)
    {
        _validationMessages.Clear(field);

        if (field.Model is InputDialogInputViewModel inputModel)
        {
            if (IsMissingRequiredValue(inputModel))
            {
                _validationMessages.Add(field, $"{inputModel.Input.Label} is required.");
            }
        }

        _editContext.NotifyValidationStateChanged();
    }

    private static FieldIdentifier GetFieldIdentifier(InputDialogInputViewModel inputModel)
    {
        var fieldName = inputModel.Input.InputType switch
        {
            InputType.Checkbox => nameof(inputModel.IsChecked),
            InputType.Number => nameof(inputModel.NumberValue),
            _ => nameof(inputModel.Value)
        };
        return new FieldIdentifier(inputModel, fieldName);
    }

    private static bool IsMissingRequiredValue(InputDialogInputViewModel inputModel)
    {
        return inputModel.Input.Required &&
            inputModel.Input.InputType != InputType.Checkbox &&
            string.IsNullOrWhiteSpace(inputModel.Value);
    }

    private async Task OkAsync()
    {
        // The workflow is:
        // 1. Validate the model that required fields are present.
        // 2. Run submit callback. Sends input values to the server.
        // 3. If validation on the server passes, a completion dialog is send back to the client which closes the dialog.
        if (_editContext.Validate())
        {
            await Content.OnSubmitCallback(Content.Interaction);
        }
    }

    private async Task CancelAsync()
    {
        await Dialog.CancelAsync();
    }
}
