// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Utils;

/// <summary>
/// Extension methods for handling CLI errors.
/// </summary>
internal static class ErrorHandlingExtensions
{
    /// <summary>
    /// Handles an exception by logging it and displaying a user-friendly error message.
    /// </summary>
    /// <param name="errorLogger">The error logger service.</param>
    /// <param name="interactionService">The interaction service for displaying messages.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="commandContext">Optional context about the command being executed.</param>
    /// <param name="verbose">Whether to display verbose error information.</param>
    /// <remarks>
    /// This method displays the log file path each time it's called. If handling multiple exceptions,
    /// consider logging them separately and showing the log file path once at the end.
    /// </remarks>
    public static void HandleException(
        this IErrorLogger errorLogger,
        IInteractionService interactionService,
        Exception exception,
        string? commandContext = null,
        bool verbose = false)
    {
        ArgumentNullException.ThrowIfNull(errorLogger);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(exception);

        // Log the full error to file
        var logFilePath = errorLogger.LogError(exception, commandContext);

        // Display user-friendly error message
        if (verbose)
        {
            // In verbose mode, show the full exception details
            interactionService.DisplayError(exception.ToString());
        }
        else
        {
            // In normal mode, show just the message
            var message = string.Format(
                CultureInfo.CurrentCulture,
                ErrorStrings.UnexpectedError,
                exception.Message);
            interactionService.DisplayError(message);
            
            // Tell user about verbose flag
            interactionService.DisplayMessage("info", ErrorStrings.UseVerboseForDetails);
        }

        // Always show where the full error was logged
        var logMessage = string.Format(
            CultureInfo.CurrentCulture,
            ErrorStrings.ErrorLoggedToFile,
            logFilePath);
        interactionService.DisplayMessage("info", logMessage);

        // Show troubleshooting link
        interactionService.DisplayMessage("info", ErrorStrings.TroubleshootingGuideUrl);
    }

    /// <summary>
    /// Handles an error message by logging it and displaying it to the user.
    /// </summary>
    /// <param name="errorLogger">The error logger service.</param>
    /// <param name="interactionService">The interaction service for displaying messages.</param>
    /// <param name="message">The error message.</param>
    /// <param name="details">Optional additional details.</param>
    /// <param name="commandContext">Optional context about the command being executed.</param>
    public static void HandleError(
        this IErrorLogger errorLogger,
        IInteractionService interactionService,
        string message,
        string? details = null,
        string? commandContext = null)
    {
        ArgumentNullException.ThrowIfNull(errorLogger);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(message);

        // Log the error to file
        var logFilePath = errorLogger.LogError(message, details, commandContext);

        // Display the error message
        interactionService.DisplayError(message);

        if (!string.IsNullOrEmpty(details))
        {
            interactionService.DisplayMessage("info", details);
        }

        // Always show where the full error was logged
        var logMessage = string.Format(
            CultureInfo.CurrentCulture,
            ErrorStrings.ErrorLoggedToFile,
            logFilePath);
        interactionService.DisplayMessage("info", logMessage);

        // Show troubleshooting link
        interactionService.DisplayMessage("info", ErrorStrings.TroubleshootingGuideUrl);
    }
}
