// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Backchannel;

namespace Aspire.Cli.Utils;

internal static class CliPathHelper
{
    internal static string GetAspireHomeDirectory(string? homeDirectory = null)
        => Path.Combine(GetUserHomeDirectory(homeDirectory), ".aspire");

    internal static string GetCliHomeDirectory(string? homeDirectory = null)
        => Path.Combine(GetAspireHomeDirectory(homeDirectory), "cli");

    internal static string GetCliRuntimeDirectory(string? homeDirectory = null)
        => Path.Combine(GetCliHomeDirectory(homeDirectory), "runtime");

    internal static string GetCliSocketDirectory(string? homeDirectory = null)
        => Path.Combine(GetCliRuntimeDirectory(homeDirectory), "sockets");

    internal static string GetCliNuGetPackagesDirectory(string? homeDirectory = null)
        => Path.Combine(GetCliHomeDirectory(homeDirectory), "nuget", "packages");

    internal static string CreateSocketPath(string socketPrefix, string? homeDirectory = null)
    {
        var socketName = $"{socketPrefix}.{CreateRandomHash()}";

        if (OperatingSystem.IsWindows())
        {
            return socketName;
        }

        var socketDirectory = GetCliSocketDirectory(homeDirectory);
        Directory.CreateDirectory(socketDirectory);
        return Path.Combine(socketDirectory, socketName);
    }

    internal static string CreateRandomHash(int length = BackchannelConstants.CompactIdentifierLength)
    {
        return BackchannelConstants.CreateRandomIdentifier(length);
    }

    private static string GetUserHomeDirectory(string? homeDirectory)
        => !string.IsNullOrWhiteSpace(homeDirectory)
            ? homeDirectory
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
}
