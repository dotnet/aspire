// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.SecretManager.Tools.Internal;

/// <summary>
/// Provides synchronized access to user secrets files within the same process.
/// Both SecretsStore and UserSecretsDeploymentStateManager use this to ensure writes are serialized.
/// </summary>
internal static class UserSecretsFileLock
{
    // Static semaphore dictionary to synchronize access per file path
    private static readonly Dictionary<string, SemaphoreSlim> s_semaphores = new();
    private static readonly object s_semaphoresLock = new();

    /// <summary>
    /// Gets a semaphore for the specified user secrets file path.
    /// </summary>
    /// <param name="filePath">The full path to the user secrets file.</param>
    /// <returns>A SemaphoreSlim that can be used for synchronization.</returns>
    public static SemaphoreSlim GetSemaphore(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        // Normalize the path to ensure consistent locking across different path representations
        var normalizedPath = Path.GetFullPath(filePath);

        lock (s_semaphoresLock)
        {
            if (!s_semaphores.TryGetValue(normalizedPath, out var semaphore))
            {
                semaphore = new SemaphoreSlim(1, 1);
                s_semaphores[normalizedPath] = semaphore;
            }
            return semaphore;
        }
    }
}
