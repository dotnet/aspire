// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

/// <summary>
/// Provides helper methods for looking up executables on the system PATH.
/// </summary>
internal static class PathLookupHelper
{
    /// <summary>
    /// Finds the full path of a command by searching the system PATH.
    /// On Windows, this also searches for executables with common extensions (.exe, .cmd, .bat, etc.).
    /// </summary>
    /// <param name="command">The command name to search for.</param>
    /// <returns>The full path to the executable if found; otherwise, <c>null</c>.</returns>
    public static string? FindFullPathFromPath(string command)
    {
        return FindFullPathFromPath(command, Environment.GetEnvironmentVariable("PATH"), Path.PathSeparator, File.Exists);
    }

    internal static string? FindFullPathFromPath(string command, string? pathVariable, char pathSeparator, Func<string, bool> fileExists)
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
            foreach (var extension in Environment.GetEnvironmentVariable("PATHEXT")?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [])
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
        foreach (var directory in (pathVariable ?? string.Empty).Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries))
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
