#pragma warning disable ASPIREINTERACTION001

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Orchestrator;

/// <summary>
/// Handles processing of parameter resources during application orchestration.
/// </summary>
internal sealed class ParameterProcessor(
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService,
    IInteractionService interactionService,
    ILogger<ParameterProcessor> logger)
{
    private readonly ResourceNotificationService _notificationService = notificationService;
    private readonly ResourceLoggerService _loggerService = loggerService;
    private readonly List<ParameterResource> _unresolvedParameters = [];

    public async Task InitializeParametersAsync(IEnumerable<ParameterResource> parameterResources)
    {
        // Initialize all parameter resources by setting their WaitForValueTcs.
        // This allows them to be processed asynchronously later.
        foreach (var parameterResource in parameterResources)
        {
            parameterResource.WaitForValueTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            await ProcessParameterAsync(parameterResource).ConfigureAwait(false);
        }

        // If interaction service is available, we can handle unresolved parameters.
        // This will allow the user to provide values for parameters that could not be initialized.
        if (interactionService.IsAvailable)
        {
            // All parameters have been processed, we can now handle unresolved parameters if any.
            if (_unresolvedParameters.Count > 0)
            {
                // Start the loop that will allow the user to specify values for unresolved parameters.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await HandleUnresolvedParametersAsync().ConfigureAwait(false);

                        logger.LogDebug("All unresolved parameters have been handled successfully.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to handle unresolved parameters");
                    }
                });
            }
        }
    }

    private async Task ProcessParameterAsync(ParameterResource parameterResource)
    {
        try
        {
            var value = parameterResource.Value ?? "";

            await _notificationService.PublishUpdateAsync(parameterResource, s =>
            {
                return s with
                {
                    Properties = s.Properties.SetResourceProperty(KnownProperties.Parameter.Value, value, parameterResource.Secret),
                    State = new(KnownResourceStates.Active, KnownResourceStateStyles.Success)
                };
            })
            .ConfigureAwait(false);

            parameterResource.WaitForValueTcs?.SetResult(value);
        }
        catch (Exception ex)
        {
            // Missing parameter values throw a MissingParameterValueException.
            if (interactionService.IsAvailable && ex is MissingParameterValueException)
            {
                // If interaction service is available, we can prompt the user to provide a value.
                // Add the parameter to unresolved parameters list.
                _unresolvedParameters.Add(parameterResource);

                _loggerService.GetLogger(parameterResource)
                    .LogWarning(ex, "Parameter resource {ResourceName} could not be initialized. Waiting for user input.", parameterResource.Name);
            }
            else
            {
                // If interaction service is not available, we log the error and set the state to error.
                parameterResource.WaitForValueTcs?.SetException(ex);

                _loggerService.GetLogger(parameterResource)
                    .LogError(ex, "Failed to initialize parameter resource {ResourceName}", parameterResource.Name);
            }

            var stateText = ex is MissingParameterValueException ?
                "Value missing" :
                "Error initializing parameter";

            await _notificationService.PublishUpdateAsync(parameterResource, s =>
            {
                return s with
                {
                    State = new(stateText, KnownResourceStateStyles.Error),
                    Properties = s.Properties.SetResourceProperty(KnownProperties.Parameter.Value, ex.Message),
                    IsHidden = false
                };
            })
            .ConfigureAwait(false);
        }
    }

    public async Task HandleUnresolvedParametersAsync()
    {
        // This method will continue in a loop until all unresolved parameters are resolved.
        while (_unresolvedParameters.Count > 0)
        {
            // First we show a notification that there are unresolved parameters.
            var result = await interactionService.PromptMessageBarAsync(
                 "Unresolved Parameters",
                 "There are unresolved parameters that need to be set. Please provide values for them.",
                 new MessageBarInteractionOptions
                 {
                     Intent = MessageIntent.Warning,
                     PrimaryButtonText = "Enter Values"
                 })
                 .ConfigureAwait(false);

            if (result.Data)
            {
                // Now we build up a new form base on the unresolved parameters.
                var inputs = new List<InteractionInput>();

                foreach (var parameter in _unresolvedParameters)
                {
                    // Create an input for each unresolved parameter.
                    inputs.Add(new InteractionInput
                    {
                        InputType = parameter.Secret ? InputType.SecretText : InputType.Text,
                        Label = parameter.Name,
                        Placeholder = "Enter value for " + parameter.Name,
                        Required = true,
                    });
                }

                var valuesPrompt = await interactionService.PromptInputsAsync(
                    "Set Unresolved Parameters",
                    "Please provide values for the unresolved parameters.",
                    [.. inputs],
                    new InputsDialogInteractionOptions
                    {
                        PrimaryButtonText = "Submit",
                        ShowDismiss = true
                    })
                    .ConfigureAwait(false);

                if (!valuesPrompt.Canceled)
                {
                    // Iterate through the unresolved parameters and set their values based on user input.
                    for (var i = _unresolvedParameters.Count - 1; i >= 0; i--)
                    {
                        var parameter = _unresolvedParameters[i];
                        var inputValue = valuesPrompt.Data[i].Value;

                        if (inputValue is null)
                        {
                            // If the input value is null, we skip this parameter.
                            continue;
                        }

                        parameter.WaitForValueTcs?.TrySetResult(inputValue);

                        // Update the parameter resource state to active with the provided value.
                        await _notificationService.PublishUpdateAsync(parameter, s =>
                        {
                            return s with
                            {
                                Properties = s.Properties.SetResourceProperty(KnownProperties.Parameter.Value, inputValue, parameter.Secret),
                                State = new(KnownResourceStates.Active, KnownResourceStateStyles.Success)
                            };
                        })
                        .ConfigureAwait(false);

                        // Remove the parameter from unresolved parameters list.
                        _unresolvedParameters.RemoveAt(i);
                    }
                }
            }
        }
    }
}
