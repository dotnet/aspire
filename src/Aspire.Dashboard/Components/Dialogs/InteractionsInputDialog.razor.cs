// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.DashboardService.Proto.V1;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Dialogs;

public partial class InteractionsInputDialog
{
    [Parameter]
    public InteractionsInputsDialogViewModel Content { get; set; } = default!;

    [CascadingParameter]
    public FluentDialog Dialog { get; set; } = default!;

    private InteractionsInputsDialogViewModel? _content;
    private EditContext _editContext = default!;
    private ValidationMessageStore _validationMessages = default!;
    private List<InputViewModel> _inputDialogInputViewModels = default!;
    private Dictionary<InputViewModel, FluentComponentBase> _elementRefs = default!;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(Content);
        _validationMessages = new ValidationMessageStore(_editContext);

        _editContext.OnValidationRequested += (s, e) => ValidateModel();
        _editContext.OnFieldChanged += (s, e) => ValidateField(e.FieldIdentifier);

        _elementRefs = new();
    }

    protected override void OnParametersSet()
    {
        if (_content != Content)
        {
            _content = Content;
            _inputDialogInputViewModels = Content.Inputs.Select(input => new InputViewModel(input)).ToList();

            AddValidationErrorsFromModel();

            Content.OnInteractionUpdated = async () =>
            {
                AddValidationErrorsFromModel();

                await InvokeAsync(StateHasChanged);
            };
        }
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Focus the first input when the dialog loads.
            if (_inputDialogInputViewModels.Count > 0 && _elementRefs.TryGetValue(_inputDialogInputViewModels[0], out var firstInputElement))
            {
                if (firstInputElement is FluentInputBase<string> textInput)
                {
                    textInput.FocusAsync();
                }
                else if (firstInputElement is FluentInputBase<bool> boolInput)
                {
                    boolInput.FocusAsync();
                }
                else if (firstInputElement is FluentInputBase<int?> numberInput)
                {
                    numberInput.FocusAsync();
                }
            }
        }

        return Task.CompletedTask;
    }

    private void AddValidationErrorsFromModel()
    {
        for (var i = 0; i < Content.Inputs.Count; i++)
        {
            var inputModel = Content.Inputs[i];
            var inputViewModel = _inputDialogInputViewModels[i];

            inputViewModel.SetInput(inputModel);

            var field = GetFieldIdentifier(inputViewModel);
            foreach (var validationError in inputModel.ValidationErrors)
            {
                _validationMessages.Add(field, validationError);
            }
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

        if (field.Model is InputViewModel inputModel)
        {
            if (IsMissingRequiredValue(inputModel))
            {
                _validationMessages.Add(field, $"{inputModel.Input.Label} is required.");
            }
        }

        _editContext.NotifyValidationStateChanged();
    }

    private static FieldIdentifier GetFieldIdentifier(InputViewModel inputModel)
    {
        var fieldName = inputModel.Input.InputType switch
        {
            InputType.Boolean => nameof(inputModel.IsChecked),
            InputType.Number => nameof(inputModel.NumberValue),
            _ => nameof(inputModel.Value)
        };
        return new FieldIdentifier(inputModel, fieldName);
    }

    private static bool IsMissingRequiredValue(InputViewModel inputModel)
    {
        return inputModel.Input.Required &&
            inputModel.Input.InputType != InputType.Boolean &&
            string.IsNullOrWhiteSpace(inputModel.Value);
    }

    private async Task SubmitAsync()
    {
        // The workflow is:
        // 1. Validate the model that required fields are present.
        // 2. Run submit callback. Sends input values to the server.
        // 3. If validation on the server passes, a completion dialog is send back to the client which closes the dialog.
        // 4. If validation fails, the server sends back validation errors which are displayed in the dialog.
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
