// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Spectre.Console;

namespace Aspire.Cli.Diagnostics;

/// <summary>
/// Switches between clean and verbose error display based on context.
/// </summary>
internal static class ErrorDisplayHelper
{
    /// <summary>
    /// Displays an exception with verbose details if enabled in context.
    /// </summary>
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

        // Always show log file path (matches aspire run style)
        if (!string.IsNullOrEmpty(bundlePath))
        {
            interactionService.DisplaySubtleMessage($"\n        Logs:  {bundlePath}/aspire.log", escapeMarkup: false);
        }

        // Always show troubleshooting link
        var troubleshootingLink = TroubleshootingLinks.GetLinkForExitCode(exitCode);
        interactionService.DisplaySubtleMessage($"\nFor troubleshooting, see: {troubleshootingLink}", escapeMarkup: false);
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
