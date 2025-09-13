#pragma warning disable ASPIREINTERACTION001

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.SecretManager.Tools.Internal;

namespace Aspire.Hosting;

/// <summary>
/// Handles processing of parameter resources during application orchestration.
/// </summary>
[Experimental("ASPIREINTERACTION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class ParameterProcessor(
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService,
    IInteractionService interactionService,
    ILogger<ParameterProcessor> logger,
    DistributedApplicationOptions options)
{
    private readonly List<ParameterResource> _unresolvedParameters = [];

    /// <summary>
    /// Initializes parameter resources and handles unresolved parameters if interaction service is available.
    /// </summary>
    /// <param name="parameterResources">The parameter resources to initialize.</param>
    /// <param name="waitForResolution">Whether to wait for all parameters to be resolved before completing the returned Task.</param>
    /// <returns>A task that completes when all parameters are resolved (if waitForResolution is true) or when initialization is complete.</returns>
    public async Task InitializeParametersAsync(IEnumerable<ParameterResource> parameterResources, bool waitForResolution = false)
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
        if (interactionService.IsAvailable && _unresolvedParameters.Count > 0)
        {
            // Start the loop that will allow the user to specify values for unresolved parameters.
            var parameterResolutionTask = Task.Run(async () =>
            {
                try
                {
                    await HandleUnresolvedParametersAsync().ConfigureAwait(false);
                    logger.LogDebug("All unresolved parameters have been handled successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to handle unresolved parameters.");
                }
            });

            if (waitForResolution)
            {
                await parameterResolutionTask.ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessParameterAsync(ParameterResource parameterResource)
    {
        try
        {
            var value = parameterResource.ValueInternal ?? "";

            await notificationService.PublishUpdateAsync(parameterResource, s =>
            {
                return s with
                {
                    Properties = s.Properties.SetResourceProperty(KnownProperties.Parameter.Value, value, parameterResource.Secret),
                    State = KnownResourceStates.Running
                };
            })
            .ConfigureAwait(false);

            parameterResource.WaitForValueTcs?.TrySetResult(value);
        }
        catch (Exception ex)
        {
            // Missing parameter values throw a MissingParameterValueException.
            if (interactionService.IsAvailable && ex is MissingParameterValueException)
            {
                // If interaction service is available, we can prompt the user to provide a value.
                // Add the parameter to unresolved parameters list.
                _unresolvedParameters.Add(parameterResource);

                loggerService.GetLogger(parameterResource)
                    .LogWarning("Parameter resource {ResourceName} could not be initialized. Waiting for user input.", parameterResource.Name);
            }
            else
            {
                // If interaction service is not available, we log the error and set the state to error.
                parameterResource.WaitForValueTcs?.TrySetException(ex);

                loggerService.GetLogger(parameterResource)
                    .LogError(ex, "Failed to initialize parameter resource {ResourceName}.", parameterResource.Name);
            }

            var stateText = ex is MissingParameterValueException ?
                "Value missing" :
                "Error initializing parameter";

            await notificationService.PublishUpdateAsync(parameterResource, s =>
            {
                return s with
                {
                    State = new(stateText, KnownResourceStateStyles.Error),
                    Properties = s.Properties.SetResourceProperty(KnownProperties.Parameter.Value, ex.Message)
                };
            })
            .ConfigureAwait(false);
        }
    }

    // Internal for testing purposes.
    private async Task HandleUnresolvedParametersAsync()
    {
        await HandleUnresolvedParametersAsync(_unresolvedParameters).ConfigureAwait(false);
    }

    // Internal for testing purposes - allows passing specific parameters to test.
    internal async Task HandleUnresolvedParametersAsync(IList<ParameterResource> unresolvedParameters)
    {
        // This method will continue in a loop until all unresolved parameters are resolved.
        while (unresolvedParameters.Count > 0)
        {
            // First we show a notification that there are unresolved parameters.
            var result = await interactionService.PromptNotificationAsync(
                InteractionStrings.ParametersBarTitle,
                InteractionStrings.ParametersBarMessage,
                 new NotificationInteractionOptions
                 {
                     Intent = MessageIntent.Warning,
                     PrimaryButtonText = InteractionStrings.ParametersBarPrimaryButtonText
                 })
                .ConfigureAwait(false);

            if (result.Data)
            {
                // Now we build up a new form base on the unresolved parameters.
                var resourceInputs = new List<(ParameterResource ParameterResource, InteractionInput Input)>();

                foreach (var parameter in unresolvedParameters)
                {
                    // Create an input for each unresolved parameter.
                    var input = parameter.CreateInput();
                    resourceInputs.Add((parameter, input));
                }

                var saveParameters = new InteractionInput
                {
                    Name = "RememberParameters",
                    InputType = InputType.Boolean,
                    Label = InteractionStrings.ParametersInputsRememberLabel
                };

                var valuesPrompt = await interactionService.PromptInputsAsync(
                    InteractionStrings.ParametersInputsTitle,
                    InteractionStrings.ParametersInputsMessage,
                    [.. resourceInputs.Select(i => i.Input), saveParameters],
                    new InputsDialogInteractionOptions
                    {
                        PrimaryButtonText = InteractionStrings.ParametersInputsPrimaryButtonText,
                        ShowDismiss = true,
                        EnableMessageMarkdown = true,
                    })
                    .ConfigureAwait(false);

                if (!valuesPrompt.Canceled)
                {
                    // Iterate through the unresolved parameters and set their values based on user input.
                    for (var i = resourceInputs.Count - 1; i >= 0; i--)
                    {
                        var (parameter, input) = (resourceInputs[i].ParameterResource, resourceInputs[i].Input);
                        var inputValue = input.Value;

                        if (string.IsNullOrEmpty(inputValue))
                        {
                            // If the input value is null, we skip this parameter.
                            continue;
                        }

                        parameter.WaitForValueTcs?.TrySetResult(inputValue);

                        // Update the parameter resource state to active with the provided value.
                        await notificationService.PublishUpdateAsync(parameter, s =>
                        {
                            return s with
                            {
                                Properties = s.Properties.SetResourceProperty(KnownProperties.Parameter.Value, inputValue, parameter.Secret),
                                State = KnownResourceStates.Running
                            };
                        })
                        .ConfigureAwait(false);

                        // Log that the parameter has been resolved
                        loggerService.GetLogger(parameter)
                            .LogInformation("Parameter resource {ResourceName} has been resolved via user interaction.", parameter.Name);

                        // Persist the parameter value to user secrets if requested.
                        if (bool.TryParse(saveParameters.Value, out var saveToSecrets) && saveToSecrets)
                        {
                            SecretsStore.TrySetUserSecret(options.Assembly, parameter.ConfigurationKey, inputValue);
                        }

                        // Remove the parameter from unresolved parameters list.
                        unresolvedParameters.RemoveAt(i);
                    }
                }
            }
        }
    }
}
