// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

/// <summary>
/// Helper for executing resource commands via the backchannel.
/// Provides common functionality for start/stop/restart/command operations.
/// </summary>
internal static class ResourceCommandHelper
{
    /// <summary>
    /// Executes a resource command and handles the response with appropriate user feedback.
    /// </summary>
    /// <param name="connection">The backchannel connection to use.</param>
    /// <param name="interactionService">The interaction service for user feedback.</param>
    /// <param name="logger">The logger for debug output.</param>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="commandName">The command to execute (e.g., "start").</param>
    /// <param name="progressVerb">The verb to display during progress (e.g., "Starting", "Stopping").</param>
    /// <param name="baseVerb">The base verb for error messages (e.g., "start", "stop").</param>
    /// <param name="pastTenseVerb">The past tense verb for success messages (e.g., "started", "stopped").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code indicating success or failure.</returns>
    public static async Task<int> ExecuteResourceCommandAsync(
        IAppHostAuxiliaryBackchannel connection,
        IInteractionService interactionService,
        ILogger logger,
        string resourceName,
        string commandName,
        string progressVerb,
        string baseVerb,
        string pastTenseVerb,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("{Verb} resource '{ResourceName}'", progressVerb, resourceName);

        var response = await interactionService.ShowStatusAsync(
            $"{progressVerb} resource '{resourceName}'...",
            async () => await connection.ExecuteResourceCommandAsync(resourceName, commandName, cancellationToken));

        return HandleResponse(response, interactionService, resourceName, progressVerb, baseVerb, pastTenseVerb);
    }

    /// <summary>
    /// Executes a generic command and handles the response with appropriate user feedback.
    /// </summary>
    public static async Task<int> ExecuteGenericCommandAsync(
        IAppHostAuxiliaryBackchannel connection,
        IInteractionService interactionService,
        ILogger logger,
        string resourceName,
        string commandName,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Executing command '{CommandName}' on resource '{ResourceName}'", commandName, resourceName);

        var response = await interactionService.ShowStatusAsync(
            $"Executing command '{commandName}' on resource '{resourceName}'...",
            async () => await connection.ExecuteResourceCommandAsync(resourceName, commandName, cancellationToken));

        if (response.Success)
        {
            interactionService.DisplaySuccess(string.Format(CultureInfo.CurrentCulture, ResourceCommandStrings.CommandExecutedSuccessfully, commandName, resourceName));
            return ExitCodeConstants.Success;
        }
        else if (response.Canceled)
        {
            interactionService.DisplayMessage(KnownEmojis.Warning, string.Format(CultureInfo.CurrentCulture, ResourceCommandStrings.CommandCanceled, commandName, resourceName));
            return ExitCodeConstants.FailedToExecuteResourceCommand;
        }
        else
        {
            var errorMessage = GetFriendlyErrorMessage(response.ErrorMessage);
            interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ResourceCommandStrings.FailedToExecuteCommand, commandName, resourceName, errorMessage));
            return ExitCodeConstants.FailedToExecuteResourceCommand;
        }
    }

    private static int HandleResponse(
        ExecuteResourceCommandResponse response,
        IInteractionService interactionService,
        string resourceName,
        string progressVerb,
        string baseVerb,
        string pastTenseVerb)
    {
        if (response.Success)
        {
            interactionService.DisplaySuccess(string.Format(CultureInfo.CurrentCulture, ResourceCommandStrings.ResourceActionSuccessful, resourceName, pastTenseVerb));
            return ExitCodeConstants.Success;
        }
        else if (response.Canceled)
        {
            interactionService.DisplayMessage(KnownEmojis.Warning, string.Format(CultureInfo.CurrentCulture, ResourceCommandStrings.ResourceActionCanceled, progressVerb, resourceName));
            return ExitCodeConstants.FailedToExecuteResourceCommand;
        }
        else
        {
            var errorMessage = GetFriendlyErrorMessage(response.ErrorMessage);
            interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ResourceCommandStrings.FailedToActionResource, baseVerb, resourceName, errorMessage));
            return ExitCodeConstants.FailedToExecuteResourceCommand;
        }
    }

    private static string GetFriendlyErrorMessage(string? errorMessage)
    {
        return string.IsNullOrEmpty(errorMessage) ? "Unknown error occurred." : errorMessage;
    }
}
