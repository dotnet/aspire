// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Bundles;

/// <summary>
/// A cross-process lock backed by an exclusive file handle.
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
/// the <see cref="FileLock"/> is disposed.
/// </para>
/// </remarks>
internal sealed class FileLock : IDisposable
{
    private readonly FileStream _stream;

    private FileLock(FileStream stream)
    {
        _stream = stream;
    }

    /// <summary>
    /// Acquires an exclusive file lock, retrying until the lock is available or the timeout expires.
    /// </summary>
    /// <param name="directory">The directory in which to create the lock file.</param>
    /// <param name="fileName">The name of the lock file.</param>
    /// <param name="timeout">Maximum time to wait for the lock. Defaults to 2 minutes.</param>
    /// <returns>A <see cref="FileLock"/> that releases the lock when disposed.</returns>
    /// <exception cref="TimeoutException">Thrown when the lock cannot be acquired within the timeout period.</exception>
    public static FileLock Acquire(string directory, string fileName = ".lock", TimeSpan? timeout = null)
    {
        Directory.CreateDirectory(directory);
        var lockPath = Path.Combine(directory, fileName);
        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(2);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // On Windows, opening a file with FileShare.None throws IOException immediately
        // instead of blocking. Retry with a short delay until the lock is available.
        while (true)
        {
            try
            {
                var stream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                return new FileLock(stream);
            }
            catch (IOException) when (stopwatch.Elapsed < effectiveTimeout)
            {
                Thread.Sleep(200);
            }
        }
    }

    public void Dispose()
    {
        _stream.Dispose();
    }
}
