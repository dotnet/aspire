// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;

namespace Aspire.Hosting.Utils;

internal static class BackchannelHelper
{
    /// <summary>
    /// Generates a predictable socket path based on the AppHost project file path.
    /// This ensures that CLI commands can reliably connect to the same AppHost instance.
    /// </summary>
    /// <param name="appHostProjectPath">Full path to the AppHost project file</param>
    /// <returns>Deterministic socket path for the AppHost</returns>
    public static string GetSocketPath(string appHostProjectPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(appHostProjectPath);
        
        // Normalize the path to ensure consistency across platforms
        var normalizedPath = Path.GetFullPath(appHostProjectPath).Replace('\\', '/');
        
        // Generate a SHA256 hash of the normalized path for uniqueness
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath));
        var hashString = Convert.ToHexString(hash)[..16]; // Use first 16 chars for brevity
        
        // Create socket directory in user's aspire directory
        var aspireDir = GetUserAspireDirectory();
        var socketDir = Path.Combine(aspireDir, "sockets");
        Directory.CreateDirectory(socketDir);
        
        // Return the socket path
        return Path.Combine(socketDir, $"{hashString}.sock");
    }
    
    /// <summary>
    /// Checks if an AppHost is currently running by checking if the socket file exists.
    /// </summary>
    /// <param name="appHostProjectPath">Full path to the AppHost project file</param>
    /// <returns>True if the socket exists and AppHost is likely running</returns>
    public static bool IsAppHostRunning(string appHostProjectPath)
    {
        var socketPath = GetSocketPath(appHostProjectPath);
        return File.Exists(socketPath);
    }
    
    private static string GetUserAspireDirectory()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDirectory, ".aspire");
    }
}