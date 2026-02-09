// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Utils;

/// <summary>
/// Provides retry logic for file operations that may fail due to transient file locks.
/// Based on patterns from dotnet/sdk FileAccessRetrier.
/// </summary>
internal static class FileAccessRetrier
{
    /// <summary>
    /// Retries an action on file access failure (IOException, UnauthorizedAccessException).
    /// </summary>
    /// <param name="action">The action to perform.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds (doubles with each retry).</param>
    public static void RetryOnFileAccessFailure(Action action, int maxRetries = 10, int initialDelayMs = 10)
    {
        var remainingRetries = maxRetries;
        var delayMs = initialDelayMs;

        while (true)
        {
            try
            {
                action();
                return;
            }
            catch (IOException) when (remainingRetries > 0)
            {
                Thread.Sleep(delayMs);
                delayMs *= 2;
                remainingRetries--;
            }
            catch (UnauthorizedAccessException) when (remainingRetries > 0)
            {
                Thread.Sleep(delayMs);
                delayMs *= 2;
                remainingRetries--;
            }
        }
    }

    /// <summary>
    /// Retries an async action on file access failure.
    /// </summary>
    /// <param name="action">The async action to perform.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds (doubles with each retry).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task RetryOnFileAccessFailureAsync(
        Func<Task> action,
        int maxRetries = 10,
        int initialDelayMs = 10,
        CancellationToken cancellationToken = default)
    {
        var remainingRetries = maxRetries;
        var delayMs = initialDelayMs;

        while (true)
        {
            try
            {
                await action();
                return;
            }
            catch (IOException) when (remainingRetries > 0)
            {
                await Task.Delay(delayMs, cancellationToken);
                delayMs *= 2;
                remainingRetries--;
            }
            catch (UnauthorizedAccessException) when (remainingRetries > 0)
            {
                await Task.Delay(delayMs, cancellationToken);
                delayMs *= 2;
                remainingRetries--;
            }
        }
    }

    /// <summary>
    /// Safely moves a file, handling the case where the destination exists.
    /// On failure, retries with exponential backoff.
    /// </summary>
    /// <param name="sourcePath">Source file path.</param>
    /// <param name="destPath">Destination file path.</param>
    /// <param name="overwrite">Whether to overwrite the destination if it exists.</param>
    public static void SafeMoveFile(string sourcePath, string destPath, bool overwrite = true)
    {
        RetryOnFileAccessFailure(() =>
        {
            if (overwrite && File.Exists(destPath))
            {
                File.Delete(destPath);
            }
            File.Move(sourcePath, destPath);
        });
    }

    /// <summary>
    /// Safely copies a file with retry on access failure.
    /// </summary>
    /// <param name="sourcePath">Source file path.</param>
    /// <param name="destPath">Destination file path.</param>
    /// <param name="overwrite">Whether to overwrite the destination if it exists.</param>
    public static void SafeCopyFile(string sourcePath, string destPath, bool overwrite = true)
    {
        RetryOnFileAccessFailure(() =>
        {
            File.Copy(sourcePath, destPath, overwrite);
        });
    }

    /// <summary>
    /// Safely deletes a file with retry on access failure.
    /// </summary>
    /// <param name="path">File path to delete.</param>
    public static void SafeDeleteFile(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        RetryOnFileAccessFailure(() =>
        {
            File.Delete(path);
        });
    }

    /// <summary>
    /// Safely deletes a directory with retry on access failure.
    /// </summary>
    /// <param name="path">Directory path to delete.</param>
    /// <param name="recursive">Whether to delete recursively.</param>
    public static void SafeDeleteDirectory(string path, bool recursive = true)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        RetryOnFileAccessFailure(() =>
        {
            Directory.Delete(path, recursive);
        });
    }

    /// <summary>
    /// Safely moves a directory with retry on access failure.
    /// </summary>
    /// <param name="sourcePath">Source directory path.</param>
    /// <param name="destPath">Destination directory path.</param>
    public static void SafeMoveDirectory(string sourcePath, string destPath)
    {
        RetryOnFileAccessFailure(() =>
        {
            Directory.Move(sourcePath, destPath);
        });
    }

    /// <summary>
    /// Tries to perform a file operation, returning false if it fails due to file locking.
    /// </summary>
    /// <param name="action">The action to attempt.</param>
    /// <returns>True if the action succeeded, false if it failed due to file locking.</returns>
    public static bool TryFileOperation(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (IOException ex) when (IsFileLockedException(ex))
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if an IOException is due to file locking.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception indicates the file is locked.</returns>
    public static bool IsFileLockedException(IOException ex)
    {
        // Windows error codes for file locking
        const int ERROR_SHARING_VIOLATION = 32;
        const int ERROR_LOCK_VIOLATION = 33;

        var hResult = ex.HResult & 0xFFFF;
        return hResult == ERROR_SHARING_VIOLATION || hResult == ERROR_LOCK_VIOLATION;
    }
}
