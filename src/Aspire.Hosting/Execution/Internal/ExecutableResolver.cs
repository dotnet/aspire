// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Execution;

/// <summary>
/// Default implementation of <see cref="IExecutableResolver"/> that resolves executable names
/// using PATH and PATHEXT (on Windows).
/// </summary>
internal sealed class ExecutableResolver : IExecutableResolver
{
    /// <inheritdoc />
    public string ResolveOrThrow(string fileName, ShellState state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var pathExtensions = GetPathExtensions(state);

        // Stage 1: Rooted/absolute paths (e.g., "/usr/bin/docker", "C:\Program Files\Docker\docker.exe")
        if (Path.IsPathRooted(fileName))
        {
            var resolved = PathLookupHelper.TryResolveWithExtensions(fileName, File.Exists, pathExtensions);
            if (resolved is not null)
            {
                return resolved;
            }

            throw new FileNotFoundException(
                $"Executable not found: {fileName}",
                fileName);
        }

        // Stage 2: Relative paths (e.g., "./build.sh", "tools/compile", "../bin/app")
        if (fileName.Contains(Path.DirectorySeparatorChar) ||
            fileName.Contains(Path.AltDirectorySeparatorChar))
        {
            var workingDir = state.WorkingDirectory ?? Environment.CurrentDirectory;
            var fullPath = Path.GetFullPath(Path.Combine(workingDir, fileName));

            var resolved = PathLookupHelper.TryResolveWithExtensions(fullPath, File.Exists, pathExtensions);
            if (resolved is not null)
            {
                return resolved;
            }

            throw new FileNotFoundException(
                $"Executable not found: {fileName}",
                fileName);
        }

        // Stage 3: Bare command names (e.g., "docker", "dotnet", "git")
        var pathValue = state.Environment.TryGetValue("PATH", out var p) ? p
            : Environment.GetEnvironmentVariable("PATH");

        var result = PathLookupHelper.FindFullPathFromPath(
            fileName,
            pathValue,
            Path.PathSeparator,
            File.Exists,
            pathExtensions);

        if (result is not null)
        {
            return result;
        }

        throw new FileNotFoundException(
            $"Executable '{fileName}' not found in PATH.",
            fileName);
    }

    private static string[]? GetPathExtensions(ShellState state)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        // Check shell state environment first, then system environment
        var pathExtValue = state.Environment.TryGetValue("PATHEXT", out var statePathExtValue)
            ? statePathExtValue
            : Environment.GetEnvironmentVariable("PATHEXT");

        return pathExtValue?.Split(';', StringSplitOptions.RemoveEmptyEntries);
    }
}
