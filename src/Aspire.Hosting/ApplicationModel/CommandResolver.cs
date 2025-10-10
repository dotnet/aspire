// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides utilities for resolving and validating commands/executables on the local machine.
/// </summary>
public static class CommandResolver
{
    /// <summary>
    /// Attempts to resolve a command (file name or path) to a full path.
    /// </summary>
    /// <param name="command">The command string (file name or path).</param>
    /// <returns>Full path if resolved; otherwise null.</returns>
    /// <remarks>
    /// The command is considered valid if either:
    /// 1. It is an absolute or relative path (contains a directory separator) that points to an existing file, or
    /// 2. It is discoverable on the current process PATH (respecting PATHEXT on Windows).
    /// </remarks>
    public static string? ResolveCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        // If the command includes any directory separator, treat it as a path (relative or absolute)
        if (command.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
        {
            var candidate = Path.GetFullPath(command);
            return File.Exists(candidate) ? candidate : null;
        }

        // Search PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
        {
            return null;
        }

        var paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows consider PATHEXT if no extension specified
            var hasExtension = Path.HasExtension(command);
            var pathext = Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD";
            var exts = pathext.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var dir in paths)
            {
                if (hasExtension)
                {
                    var candidate = Path.Combine(dir, command);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                else
                {
                    foreach (var ext in exts)
                    {
                        var candidate = Path.Combine(dir, command + ext);
                        if (File.Exists(candidate))
                        {
                            return candidate;
                        }
                    }
                }
            }
        }
        else
        {
            foreach (var dir in paths)
            {
                var candidate = Path.Combine(dir, command);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
