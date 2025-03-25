// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.TestUtilities;

internal static class FileUtil
{
    public static string? FindFullPathFromPath(string command) => FindFullPathFromPath(command, Environment.GetEnvironmentVariable("PATH"), Path.PathSeparator, File.Exists);

    private static string? FindFullPathFromPath(string command, string? pathVariable, char pathSeparator, Func<string, bool> fileExists)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(command));

        var fullPath = FindFullPath(command, pathVariable, pathSeparator, fileExists);
        if (fullPath is not null)
        {
            return fullPath;
        }

        if (OperatingSystem.IsWindows())
        {
            // On Windows, we need to check for the command with all possible extensions.
            foreach (var extension in Environment.GetEnvironmentVariable("PATHEXT")?.Split(';') ?? Array.Empty<string>())
            {
                var fileName = command.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? command : command + extension;

                fullPath = FindFullPath(fileName, pathVariable, pathSeparator, fileExists);
                if (fullPath is not null)
                {
                    return fullPath;
                }
            }
        }

        return null;
    }

    private static string? FindFullPath(string command, string? pathVariable, char pathSeparator, Func<string, bool> fileExists)
    {
        foreach (var directory in (pathVariable ?? string.Empty).Split(pathSeparator))
        {
            var fullPath = Path.Combine(directory, command);

            if (fileExists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}
