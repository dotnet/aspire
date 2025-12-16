// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Spectre.Console;

namespace Aspire.Cli.Diagnostics;

/// <summary>
/// Helper for displaying errors with appropriate detail based on verbose mode.
/// </summary>
internal static class ErrorDisplayHelper
{
    /// <summary>
    /// Displays an exception with appropriate detail based on verbose mode.
    /// </summary>
    /// <param name="interactionService">The interaction service to use for display.</param>
    /// <param name="exception">The exception to display.</param>
    /// <param name="exitCode">The exit code that will be returned.</param>
    /// <param name="context">The CLI execution context.</param>
    /// <param name="bundlePath">Optional path to the diagnostics bundle.</param>
    public static void DisplayException(
        IInteractionService interactionService,
        Exception exception,
        int exitCode,
        CliExecutionContext context,
        string? bundlePath = null)
    {
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(context);

        if (context.VerboseMode)
        {
            // Verbose mode: show full exception details
            DisplayVerboseException(interactionService, exception);
        }
        else
        {
            // Clean mode: show just the error message
            var cleanMessage = GetCleanErrorMessage(exception);
            interactionService.DisplayError(cleanMessage.EscapeMarkup());
        }

        // Always show troubleshooting link
        var troubleshootingLink = TroubleshootingLinks.GetLinkForExitCode(exitCode);
        interactionService.DisplaySubtleMessage($"For troubleshooting, see: {troubleshootingLink}", escapeMarkup: false);

        // Show where details were saved if bundle was written
        if (!string.IsNullOrEmpty(bundlePath))
        {
            interactionService.DisplaySubtleMessage($"Details saved to: {bundlePath}", escapeMarkup: false);
        }
    }

    private static void DisplayVerboseException(IInteractionService interactionService, Exception exception)
    {
        interactionService.DisplayError($"Exception: {exception.GetType().Name}");
        interactionService.DisplayPlainText(exception.Message);
        
        if (!string.IsNullOrEmpty(exception.StackTrace))
        {
            interactionService.DisplayPlainText(string.Empty);
            interactionService.DisplaySubtleMessage("Stack Trace:", escapeMarkup: false);
            interactionService.DisplayPlainText(exception.StackTrace);
        }

        // Display inner exceptions
        var innerException = exception.InnerException;
        var depth = 1;
        while (innerException != null)
        {
            interactionService.DisplayPlainText(string.Empty);
            interactionService.DisplaySubtleMessage($"Inner Exception ({depth}):", escapeMarkup: false);
            interactionService.DisplayPlainText($"{innerException.GetType().Name}: {innerException.Message}");
            
            if (!string.IsNullOrEmpty(innerException.StackTrace))
            {
                interactionService.DisplayPlainText(innerException.StackTrace);
            }

            innerException = innerException.InnerException;
            depth++;
        }

        // Handle AggregateException specially
        if (exception is AggregateException aggregateException)
        {
            interactionService.DisplayPlainText(string.Empty);
            interactionService.DisplaySubtleMessage($"Aggregate Exception contains {aggregateException.InnerExceptions.Count} inner exception(s):", escapeMarkup: false);
            for (int i = 0; i < aggregateException.InnerExceptions.Count; i++)
            {
                var inner = aggregateException.InnerExceptions[i];
                interactionService.DisplayPlainText($"  [{i}] {inner.GetType().Name}: {inner.Message}");
            }
        }
    }

    private static string GetCleanErrorMessage(Exception exception)
    {
        // For most exceptions, the Message property provides a clean summary
        // For some exception types, we might want to customize the message
        return exception switch
        {
            OperationCanceledException => "Operation was canceled",
            TimeoutException => "Operation timed out",
            UnauthorizedAccessException => "Access denied. You may not have permission to perform this operation.",
            IOException ioEx when ioEx.Message.Contains("being used by another process") => 
                "A file is being used by another process. Please close any programs that might be using the file and try again.",
            _ => exception.Message
        };
    }
}
