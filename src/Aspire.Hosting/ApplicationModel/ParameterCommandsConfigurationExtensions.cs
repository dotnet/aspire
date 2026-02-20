// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREINTERACTION001
#pragma warning disable ASPIREUSERSECRETS001

using System.Globalization;
using Aspire.Dashboard.Model;
using Aspire.Hosting.Resources;
using Aspire.Hosting.UserSecrets;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

internal static class ParameterCommandsConfigurationExtensions
{
    internal static void AddParameterCommands(this ParameterResource resource, IUserSecretsManager userSecretsManager, IInteractionService interactionService, ResourceNotificationService notificationService, ResourceLoggerService loggerService)
    {
        // Edit command - allows editing the parameter value via the interaction service
        resource.Annotations.Add(new ResourceCommandAnnotation(
            name: KnownResourceCommands.EditParameterCommand,
            displayName: CommandStrings.EditParameterName,
            executeCommand: async context =>
            {
                if (!interactionService.IsAvailable)
                {
                    return CommandResults.Failure("Interaction service is not available.");
                }

                var logger = loggerService.GetLogger(resource);
                logger.LogInformation("Editing parameter value for '{ParameterName}'.", resource.Name);

                // Create an input for the parameter
                var input = resource.CreateInput();

                // Get the current value if available
                try
                {
                    var currentValue = await resource.GetValueAsync(context.CancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(currentValue) && !resource.Secret)
                    {
                        input.Value = currentValue;
                    }
                }
                catch
                {
                    // Ignore errors getting the current value
                }

                var result = await interactionService.PromptInputsAsync(
                    InteractionStrings.ParametersInputsTitle,
                    string.Format(CultureInfo.CurrentCulture, InteractionStrings.EditParameterMessage, resource.Name),
                    [input],
                    new InputsDialogInteractionOptions
                    {
                        PrimaryButtonText = InteractionStrings.ParametersInputsPrimaryButtonText,
                        ShowDismiss = true
                    }).ConfigureAwait(false);

                if (result.Canceled || string.IsNullOrEmpty(input.Value))
                {
                    return CommandResults.Canceled();
                }

                var newValue = input.Value!;

                // Save to user secrets
                var configKey = resource.ConfigurationKey;
                if (!userSecretsManager.TrySetSecret(configKey, newValue))
                {
                    logger.LogWarning("Failed to save parameter '{ParameterName}' to user secrets.", resource.Name);
                }

                // Update the parameter's TCS if it exists
                resource.WaitForValueTcs?.TrySetResult(newValue);

                // Update the resource state
                await notificationService.PublishUpdateAsync(resource, s =>
                {
                    return s with
                    {
                        Properties = s.Properties.SetResourceProperty(KnownProperties.Parameter.Value, newValue, resource.Secret),
                        State = KnownResourceStates.Running
                    };
                }).ConfigureAwait(false);

                logger.LogInformation("Parameter '{ParameterName}' value updated.", resource.Name);
                return CommandResults.Success();
            },
            updateState: context =>
            {
                // Edit is always enabled when the parameter is running
                var state = context.ResourceSnapshot.State?.Text;
                if (state == KnownResourceStates.Running)
                {
                    return ResourceCommandState.Enabled;
                }
                return ResourceCommandState.Disabled;
            },
            displayDescription: CommandStrings.EditParameterDescription,
            parameter: null,
            confirmationMessage: null,
            iconName: "Edit",
            iconVariant: IconVariant.Regular,
            isHighlighted: true));

        // Clear command - removes the parameter value from user secrets
        resource.Annotations.Add(new ResourceCommandAnnotation(
            name: KnownResourceCommands.ClearParameterCommand,
            displayName: CommandStrings.ClearParameterName,
            executeCommand: async context =>
            {
                var logger = loggerService.GetLogger(resource);
                logger.LogInformation("Clearing parameter value for '{ParameterName}'.", resource.Name);

                // Remove from user secrets
                var configKey = resource.ConfigurationKey;
                if (!userSecretsManager.TryRemoveSecret(configKey))
                {
                    logger.LogWarning("Failed to remove parameter '{ParameterName}' from user secrets.", resource.Name);
                }

                // Reset the TCS so the parameter can be re-initialized
                resource.WaitForValueTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

                // Update the resource state to show it needs a value
                await notificationService.PublishUpdateAsync(resource, s =>
                {
                    return s with
                    {
                        Properties = s.Properties.SetResourceProperty(KnownProperties.Parameter.Value, "Value cleared"),
                        State = new("Value missing", KnownResourceStateStyles.Warn)
                    };
                }).ConfigureAwait(false);

                logger.LogInformation("Parameter '{ParameterName}' value cleared from user secrets.", resource.Name);
                return CommandResults.Success();
            },
            updateState: context =>
            {
                // Clear is enabled when the parameter has a value (is running)
                var state = context.ResourceSnapshot.State?.Text;
                if (state == KnownResourceStates.Running)
                {
                    return ResourceCommandState.Enabled;
                }
                return ResourceCommandState.Hidden;
            },
            displayDescription: CommandStrings.ClearParameterDescription,
            parameter: null,
            confirmationMessage: CommandStrings.ClearParameterConfirmation,
            iconName: "Delete",
            iconVariant: IconVariant.Regular,
            isHighlighted: false));
    }
}
