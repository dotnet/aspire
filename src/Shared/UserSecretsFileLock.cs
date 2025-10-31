// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.SecretManager.Tools.Internal;

/// <summary>
/// Provides synchronized access to user secrets files within the same process.
/// Both SecretsStore and UserSecretsDeploymentStateManager use this to ensure writes are serialized.
/// </summary>
internal static class UserSecretsFileLock
{
    // Static lock dictionary to synchronize access per file path
    private static readonly Dictionary<string, object> s_locks = new();
    private static readonly object s_locksLock = new();

    /// <summary>
    /// Gets a lock object for the specified user secrets file path.
    /// </summary>
    /// <param name="filePath">The full path to the user secrets file.</param>
    /// <returns>A lock object that can be used with the lock statement.</returns>
    public static object GetLock(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        // Normalize the path to ensure consistent locking across different path representations
        var normalizedPath = Path.GetFullPath(filePath);

        lock (s_locksLock)
        {
            if (!s_locks.TryGetValue(normalizedPath, out var lockObj))
            {
                lockObj = new object();
                s_locks[normalizedPath] = lockObj;
            }
            return lockObj;
        }
    }
}
