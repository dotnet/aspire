// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.ResourceService.Proto.V1;
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

    private EditContext _editContext = default!;
    private ValidationMessageStore _validationMessages = default!;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(Content);
        _validationMessages = new ValidationMessageStore(_editContext);

        _editContext.OnValidationRequested += (s, e) => ValidateModel();
        _editContext.OnFieldChanged += (s, e) => ValidateField(e.FieldIdentifier);
    }

    private void ValidateModel()
    {
        _validationMessages.Clear();

        foreach (var inputModel in Content.Inputs)
        {
            var field = new FieldIdentifier(inputModel, nameof(inputModel.Value));
            if (IsMissingRequiredValue(inputModel))
            {
                _validationMessages.Add(field, $"{inputModel.Label} is required.");
            }
        }

        _editContext.NotifyValidationStateChanged();
    }

    private void ValidateField(FieldIdentifier field)
    {
        _validationMessages.Clear(field);

        if (field.Model is InteractionInput inputModel)
        {
            if (IsMissingRequiredValue(inputModel))
            {
                _validationMessages.Add(field, $"{inputModel.Label} is required.");
            }
        }

        _editContext.NotifyValidationStateChanged();
    }

    private static bool IsMissingRequiredValue(InteractionInput inputModel)
    {
        return inputModel.Required &&
            inputModel.InputType != InputType.Checkbox &&
            string.IsNullOrWhiteSpace(inputModel.Value);
    }

    private async Task OkAsync()
    {
        if (_editContext.Validate())
        {
            await Dialog.CloseAsync(Content);
        }
    }

    private async Task CancelAsync()
    {
        await Dialog.CancelAsync();
    }
}
