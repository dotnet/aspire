// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
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
    /// Checks if an AppHost is currently running by attempting to connect to the socket.
    /// </summary>
    /// <param name="appHostProjectPath">Full path to the AppHost project file</param>
    /// <returns>True if the socket exists and AppHost is actively listening</returns>
    public static bool IsAppHostRunning(string appHostProjectPath)
    {
        var socketPath = GetSocketPath(appHostProjectPath);
        
        // First check if the socket file exists
        if (!File.Exists(socketPath))
        {
            return false;
        }

        // Try to connect to the socket to verify something is actually listening
        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(socketPath);
            
            // Use a short timeout for the connection attempt
            socket.ReceiveTimeout = 1000; // 1 second
            socket.SendTimeout = 1000;    // 1 second
            
            socket.Connect(endpoint);
            
            // If we got here, something is listening on the socket
            return true;
        }
        catch (SocketException)
        {
            // Connection failed, either nothing listening or socket is stale
            // Clean up stale socket file if it exists
            try
            {
                if (File.Exists(socketPath))
                {
                    File.Delete(socketPath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            return false;
        }
        catch
        {
            // Any other exception means we couldn't determine the state
            return false;
        }
    }
    
    private static string GetUserAspireDirectory()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDirectory, ".aspire");
    }
}