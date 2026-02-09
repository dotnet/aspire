// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Utils;

/// <summary>
/// A cross-process file-based lock that is safe to use with async/await.
/// </summary>
/// <remarks>
/// <para>
/// This is used instead of <see cref="System.Threading.Mutex"/> because:
/// </para>
/// <list type="bullet">
/// <item>Named mutexes with <c>Global\</c> prefix behave differently on Linux vs Windows.</item>
/// <item><see cref="System.Threading.Mutex"/> has thread affinity — <c>ReleaseMutex</c> must be
/// called from the same thread that called <c>WaitOne</c>, which is incompatible with
/// async/await where continuations may run on a different thread.</item>
/// </list>
/// <para>
/// A <see cref="FileStream"/> opened with <see cref="FileShare.None"/> provides exclusive
/// access that works cross-platform and has no thread affinity. The lock is released when
/// the stream is disposed.
/// </para>
/// <para>
/// Based on the locking pattern from NuGet.Common.ConcurrencyUtilities.
/// </para>
/// </remarks>
internal static class FileLock
{
    // Short delay keeps latency low under contention without busy-spinning.
    // Matches the delay used by NuGet.Common.ConcurrencyUtilities.
    private static readonly TimeSpan s_defaultRetryDelay = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// Executes an async action while holding an exclusive file lock.
    /// </summary>
    /// <typeparam name="T">The return type of the action.</typeparam>
    /// <param name="lockPath">The full path of the lock file.</param>
    /// <param name="action">The async action to execute while holding the lock.</param>
    /// <param name="cancellationToken">Token to cancel the wait for the lock.</param>
    /// <returns>The result of the action.</returns>
    public static async Task<T> ExecuteWithLockAsync<T>(
        string lockPath,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        // Ensure the directory exists before creating the lock file
        var directory = Path.GetDirectoryName(lockPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        FileStream? lockStream = null;
        try
        {
            lockStream = await AcquireAsync(lockPath, cancellationToken).ConfigureAwait(false);

            // The action runs inside the lock — the lock is released in the finally block
            return await action(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Disposing the stream releases the OS-level file lock and deletes
            // the lock file (FileOptions.DeleteOnClose).
            lockStream?.Dispose();
        }
    }

    /// <summary>
    /// Executes a synchronous action while holding an exclusive file lock.
    /// </summary>
    /// <param name="lockPath">The full path of the lock file.</param>
    /// <param name="action">The action to execute while holding the lock.</param>
    /// <param name="cancellationToken">Token to cancel the wait for the lock.</param>
    public static void ExecuteWithLock(
        string lockPath,
        Action action,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(lockPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        FileStream? lockStream = null;
        try
        {
            lockStream = Acquire(lockPath, cancellationToken);
            action();
        }
        finally
        {
            lockStream?.Dispose();
        }
    }

    /// <summary>
    /// Acquires a file lock asynchronously, retrying on contention.
    /// Uses <see cref="Task.Delay(TimeSpan, CancellationToken)"/> between retries to avoid blocking the thread pool.
    /// </summary>
    private static async Task<FileStream> AcquireAsync(string lockPath, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return CreateLockStream(lockPath);
            }
            catch (IOException)
            {
                // Sharing violation — another process holds the lock. On Windows the
                // FileStream constructor throws immediately; on Unix it may also throw
                // if the file is exclusively locked. Wait and retry.
                await Task.Delay(s_defaultRetryDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                // Can occur transiently when the lock file is being deleted
                // (DeleteOnClose) by the process that just released the lock,
                // or if an admin/antivirus has the file temporarily locked.
                await Task.Delay(s_defaultRetryDelay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Acquires a file lock synchronously, retrying on contention.
    /// Uses <see cref="Thread.Sleep(TimeSpan)"/> between retries.
    /// </summary>
    private static FileStream Acquire(string lockPath, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return CreateLockStream(lockPath);
            }
            catch (IOException)
            {
                Thread.Sleep(s_defaultRetryDelay);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(s_defaultRetryDelay);
            }
        }
    }

    /// <summary>
    /// Opens the lock file with exclusive access. Only one process can hold the
    /// handle at a time. <see cref="FileOptions.DeleteOnClose"/> ensures the lock
    /// file is cleaned up automatically when the handle is released.
    /// </summary>
    private static FileStream CreateLockStream(string lockPath)
    {
        return new FileStream(
            lockPath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 1,
            FileOptions.DeleteOnClose);
    }
}
