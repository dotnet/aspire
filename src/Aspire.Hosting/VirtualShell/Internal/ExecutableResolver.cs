// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.VirtualShell.Internal;

/// <summary>
/// Default implementation of <see cref="IExecutableResolver"/> that resolves executable names
/// using PATH and PATHEXT (on Windows).
/// </summary>
internal sealed class ExecutableResolver : IExecutableResolver
{
    private static readonly char s_pathSeparator = OperatingSystem.IsWindows() ? ';' : ':';

    private static readonly string[] s_defaultPathExt = OperatingSystem.IsWindows()
        ? [".COM", ".EXE", ".BAT", ".CMD", ".VBS", ".VBE", ".JS", ".JSE", ".WSF", ".WSH", ".MSC", ".PS1"]
        : [];

    /// <inheritdoc />
    public string ResolveOrThrow(string fileName, ShellState state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        // If it's already a rooted path, verify it exists
        if (Path.IsPathRooted(fileName))
        {
            if (IsExecutable(fileName))
            {
                return fileName;
            }

            // On Windows, try adding extensions
            if (OperatingSystem.IsWindows())
            {
                var extensions = GetPathExtensions(state);
                foreach (var ext in extensions)
                {
                    var withExt = fileName + ext;
                    if (IsExecutable(withExt))
                    {
                        return withExt;
                    }
                }
            }

            throw new FileNotFoundException(
                $"Executable not found: {fileName}",
                fileName);
        }

        // If it contains a directory separator, treat as relative path
        if (fileName.Contains(Path.DirectorySeparatorChar) ||
            fileName.Contains(Path.AltDirectorySeparatorChar))
        {
            var workingDir = state.WorkingDirectory ?? Environment.CurrentDirectory;
            var fullPath = Path.GetFullPath(Path.Combine(workingDir, fileName));

            if (IsExecutable(fullPath))
            {
                return fullPath;
            }

            // On Windows, try adding extensions
            if (OperatingSystem.IsWindows())
            {
                var extensions = GetPathExtensions(state);
                foreach (var ext in extensions)
                {
                    var withExt = fullPath + ext;
                    if (IsExecutable(withExt))
                    {
                        return withExt;
                    }
                }
            }

            throw new FileNotFoundException(
                $"Executable not found: {fileName}",
                fileName);
        }

        // Search PATH
        var pathDirs = GetPathDirectories(state);
        var pathExtensions = OperatingSystem.IsWindows() ? GetPathExtensions(state) : [""];

        foreach (var dir in pathDirs)
        {
            foreach (var ext in pathExtensions)
            {
                var candidate = Path.Combine(dir, fileName + ext);
                if (IsExecutable(candidate))
                {
                    return candidate;
                }
            }
        }

        throw new FileNotFoundException(
            $"Executable '{fileName}' not found in PATH. " +
            $"Searched directories: {string.Join(s_pathSeparator, pathDirs)}",
            fileName);
    }

    private static string[] GetPathDirectories(ShellState state)
    {
        // Check shell state environment first, then system environment
        string? pathValue = null;

        if (state.Environment.TryGetValue("PATH", out var statePathValue))
        {
            pathValue = statePathValue;
        }

        pathValue ??= Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(pathValue))
        {
            return [];
        }

        return pathValue.Split(s_pathSeparator, StringSplitOptions.RemoveEmptyEntries);
    }

    private static string[] GetPathExtensions(ShellState state)
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        // Check shell state environment first, then system environment
        string? pathExtValue = null;

        if (state.Environment.TryGetValue("PATHEXT", out var statePathExtValue))
        {
            pathExtValue = statePathExtValue;
        }

        pathExtValue ??= Environment.GetEnvironmentVariable("PATHEXT");

        if (string.IsNullOrEmpty(pathExtValue))
        {
            return s_defaultPathExt;
        }

        // Include empty string first to check the filename as-is
        return ["", .. pathExtValue.Split(';', StringSplitOptions.RemoveEmptyEntries)];
    }

    private static bool IsExecutable(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        if (OperatingSystem.IsWindows())
        {
            // On Windows, if the file exists it's considered executable
            return true;
        }

        // On Unix, check executable permission using file info
        // We use a simple existence check here; actual execution
        // permission is checked by the OS when the process is started
        return true;
    }
}
