// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Utils;

/// <summary>
/// Helper class for managing concurrent SDK installations with file locking.
/// </summary>
internal static class SdkLockHelper
{
    /// <summary>
    /// Acquires a lock for SDK installation to prevent concurrent installations.
    /// </summary>
    /// <param name="sdkVersion">The SDK version being installed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A disposable lock that should be disposed when installation is complete.</returns>
    public static async Task<IDisposable> AcquireSdkLockAsync(string sdkVersion, CancellationToken cancellationToken = default)
    {
        var aspireHome = Environment.GetEnvironmentVariable("ASPIRE_HOME") 
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspire");
        
        var stateDir = Path.Combine(aspireHome, "state");
        Directory.CreateDirectory(stateDir);
        
        var lockFileName = $"sdk-lock-{sdkVersion}.lock";
        var lockFilePath = Path.Combine(stateDir, lockFileName);
        
        // Wait for any existing lock to be released
        var maxWaitTime = TimeSpan.FromMinutes(10); // Maximum wait time
        var startTime = DateTime.UtcNow;
        
        while (File.Exists(lockFilePath))
        {
            if (DateTime.UtcNow - startTime > maxWaitTime)
            {
                // Remove stale lock file if it's too old (more than 30 minutes)
                var lockFileInfo = new FileInfo(lockFilePath);
                if (DateTime.UtcNow - lockFileInfo.CreationTimeUtc > TimeSpan.FromMinutes(30))
                {
                    try
                    {
                        File.Delete(lockFilePath);
                        break;
                    }
                    catch
                    {
                        // If we can't delete it, another process might be using it
                    }
                }
                
                throw new TimeoutException($"Timeout waiting for SDK installation lock for version {sdkVersion}");
            }
            
            await Task.Delay(1000, cancellationToken);
        }
        
        // Create lock file
        await File.WriteAllTextAsync(lockFilePath, 
            $"Locked by process {Environment.ProcessId} at {DateTime.UtcNow:O}", 
            cancellationToken);
        
        return new SdkLock(lockFilePath);
    }

    private sealed class SdkLock : IDisposable
    {
        private readonly string _lockFilePath;
        private bool _disposed;

        public SdkLock(string lockFilePath)
        {
            _lockFilePath = lockFilePath;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    if (File.Exists(_lockFilePath))
                    {
                        File.Delete(_lockFilePath);
                    }
                }
                catch
                {
                    // Ignore errors when cleaning up lock file
                }
                
                _disposed = true;
            }
        }
    }
}