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
        var pathExtensions = OperatingSystem.IsWindows()
            ? Environment.GetEnvironmentVariable("PATHEXT")?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? []
            : null;

        return FindFullPathFromPath(command, Environment.GetEnvironmentVariable("PATH"), Path.PathSeparator, File.Exists, pathExtensions);
    }

    /// <summary>
    /// Finds the full path of a command by searching the specified PATH variable.
    /// </summary>
    /// <param name="command">The command name to search for.</param>
    /// <param name="pathVariable">The PATH environment variable value to search.</param>
    /// <param name="pathSeparator">The character used to separate paths in the PATH variable.</param>
    /// <param name="fileExists">A function to check if a file exists at a given path.</param>
    /// <param name="pathExtensions">Optional array of executable extensions to try (e.g., .exe, .cmd). When provided, these extensions will be appended to the command if not already present.</param>
    /// <returns>The full path to the executable if found; otherwise, <c>null</c>.</returns>
    internal static string? FindFullPathFromPath(string command, string? pathVariable, char pathSeparator, Func<string, bool> fileExists, string[]? pathExtensions = null)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(command));

        // If the command already has a known extension, just search for it directly.
        if (pathExtensions is not null && pathExtensions.Any(ext => command.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            return FindFullPath(command, pathVariable, pathSeparator, fileExists, pathExtensions: null);
        }

        return FindFullPath(command, pathVariable, pathSeparator, fileExists, pathExtensions);
    }

    private static string? FindFullPath(string command, string? pathVariable, char pathSeparator, Func<string, bool> fileExists, string[]? pathExtensions)
    {
        foreach (var directory in (pathVariable ?? string.Empty).Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            // On Windows, search each directory completely with all PATHEXT extensions before moving to the next.
            // This matches Windows command lookup behavior where directory order takes precedence.
            if (pathExtensions is not null && pathExtensions.Length > 0)
            {
                foreach (var extension in pathExtensions)
                {
                    var fullPathWithExt = Path.Combine(directory, command + extension);
                    if (fileExists(fullPathWithExt))
                    {
                        return fullPathWithExt;
                    }
                }
            }

            // Try exact match (for non-Windows, or as fallback on Windows if no extension match found in this directory).
            var fullPath = Path.Combine(directory, command);
            if (fileExists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}
