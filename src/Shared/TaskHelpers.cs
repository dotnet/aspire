// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Dashboard.Utils;

internal static class TaskHelpers
{
    public static Task WaitIgnoreCancelAsync(Task? task)
    {
        return WaitIgnoreCancelCoreAsync(task, logger: null, logMessage: null);
    }

    public static Task WaitIgnoreCancelAsync(Task? task, ILogger logger, string logMessage)
    {
        return WaitIgnoreCancelCoreAsync(task, logger, logMessage);
    }

    private static async Task WaitIgnoreCancelCoreAsync(Task? task, ILogger? logger = null, string? logMessage = null)
    {
        if (task is null || task.IsCompletedSuccessfully || task.IsCanceled)
        {
            return;
        }

        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignore errors from canceled tasks.
        }
        catch (Exception ex) when (logger is not null)
        {
            logger.LogError(ex, logMessage ?? "Unexpected error.");
            throw;
        }
    }
}
