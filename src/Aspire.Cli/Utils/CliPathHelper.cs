// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Backchannel;

namespace Aspire.Cli.Utils;

internal static class CliPathHelper
{
    internal static string GetAspireHomeDirectory()
        => GetAspireHomeDirectoryCore();

    /// <summary>
    /// Gets the CLI-managed NuGet packages directory used when the CLI writes a channel-specific
    /// <c>globalPackagesFolder</c> override.
    /// </summary>
    /// <param name="homeDirectory">An optional home directory override used by tests.</param>
    internal static string GetCliNuGetPackagesDirectory(string? homeDirectory = null)
        => Path.Combine(GetCliHomeDirectoryCore(homeDirectory), "nuget", "packages");

    /// <summary>
    /// Creates a randomized CLI-managed socket path.
    /// </summary>
    /// <param name="socketPrefix">The socket file prefix.</param>
    /// <param name="homeDirectory">An optional home directory override used by tests.</param>
    internal static string CreateSocketPath(string socketPrefix, string? homeDirectory = null)
    {
        var socketName = $"{socketPrefix}.{BackchannelConstants.CreateRandomIdentifier()}";

        if (OperatingSystem.IsWindows())
        {
            return socketName;
        }

        var socketDirectory = GetCliSocketDirectory(homeDirectory);
        Directory.CreateDirectory(socketDirectory);
        return Path.Combine(socketDirectory, socketName);
    }

    private static string GetAspireHomeDirectoryCore(string? homeDirectory = null)
        => Path.Combine(GetUserHomeDirectory(homeDirectory), ".aspire");

    private static string GetCliHomeDirectoryCore(string? homeDirectory = null)
        => Path.Combine(GetAspireHomeDirectoryCore(homeDirectory), "cli");

    private static string GetCliRuntimeDirectory(string? homeDirectory = null)
        => Path.Combine(GetCliHomeDirectoryCore(homeDirectory), "runtime");

    private static string GetCliSocketDirectory(string? homeDirectory = null)
        => Path.Combine(GetCliRuntimeDirectory(homeDirectory), "sockets");

    private static string GetUserHomeDirectory(string? homeDirectory)
        => !string.IsNullOrWhiteSpace(homeDirectory)
            ? homeDirectory
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
}
