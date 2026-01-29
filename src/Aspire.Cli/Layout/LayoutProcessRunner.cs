// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Shared;

namespace Aspire.Cli.Layout;

/// <summary>
/// Helper to detect the current runtime identifier.
/// Delegates to shared BundleDiscovery for consistent behavior.
/// </summary>
internal static class RuntimeIdentifierHelper
{
    /// <summary>
    /// Gets the current platform's runtime identifier.
    /// </summary>
    public static string GetCurrent() => BundleDiscovery.GetCurrentRuntimeIdentifier();

    /// <summary>
    /// Gets the archive extension for the current platform.
    /// </summary>
    public static string GetArchiveExtension() => BundleDiscovery.GetArchiveExtension();
}

/// <summary>
/// Helper to detect executable vs DLL paths cross-platform.
/// </summary>
internal static class ExecutableHelper
{
    /// <summary>
    /// Determines if a path refers to an executable (vs a DLL that needs dotnet to run).
    /// On Windows: checks for .exe extension.
    /// On Unix: checks that it's not a .dll (extensionless files are executables).
    /// </summary>
    public static bool IsExecutable(string path)
    {
        return OperatingSystem.IsWindows()
            ? path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            : !path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Utilities for running processes using the layout's .NET runtime.
/// </summary>
internal static class LayoutProcessRunner
{
    /// <summary>
    /// Checks if the muxer path is a bare command name (e.g., "dotnet") that the OS can resolve via PATH.
    /// </summary>
    private static bool IsBareCommandName(string path)
    {
        return path is "dotnet" or "dotnet.exe";
    }

    /// <summary>
    /// Validates that the muxer path is usable - either exists as a file or is a bare command name.
    /// </summary>
    private static bool IsValidMuxerPath(string? muxerPath)
    {
        if (muxerPath is null)
        {
            return false;
        }

        // Bare command names like "dotnet" are valid - the OS will resolve them via PATH
        if (IsBareCommandName(muxerPath))
        {
            return true;
        }

        // Full paths must exist
        return File.Exists(muxerPath);
    }

    /// <summary>
    /// Runs a managed DLL using the layout's .NET runtime.
    /// </summary>
    public static async Task<int> RunManagedAsync(
        LayoutConfiguration layout,
        string dllPath,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        IDictionary<string, string>? environmentVariables = null,
        CancellationToken ct = default)
    {
        var muxerPath = layout.GetMuxerPath();
        if (!IsValidMuxerPath(muxerPath))
        {
            throw new InvalidOperationException("Layout muxer not found");
        }

        using var process = new Process();
        process.StartInfo.FileName = muxerPath;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        // Set DOTNET_ROOT to use the layout's runtime
        var runtimePath = layout.GetComponentPath(LayoutComponent.Runtime);
        if (runtimePath is not null)
        {
            process.StartInfo.Environment["DOTNET_ROOT"] = runtimePath;
            // Disable multi-level lookup to avoid using global .NET
            process.StartInfo.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
        }

        // Add custom environment variables
        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                process.StartInfo.Environment[key] = value;
            }
        }

        if (workingDirectory is not null)
        {
            process.StartInfo.WorkingDirectory = workingDirectory;
        }

        // First argument is the DLL path
        process.StartInfo.ArgumentList.Add(dllPath);

        // Add remaining arguments
        foreach (var arg in arguments)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();
        await process.WaitForExitAsync(ct);

        return process.ExitCode;
    }

    /// <summary>
    /// Runs a managed DLL or native EXE and captures output.
    /// </summary>
    /// <param name="layout">The layout configuration.</param>
    /// <param name="toolPath">Path to the tool (either .dll or .exe).</param>
    /// <param name="arguments">Arguments to pass to the tool.</param>
    /// <param name="workingDirectory">Optional working directory.</param>
    /// <param name="environmentVariables">Optional environment variables.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing exit code, stdout, and stderr.</returns>
    public static async Task<(int ExitCode, string Output, string Error)> RunManagedWithOutputAsync(
        LayoutConfiguration layout,
        string toolPath,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        IDictionary<string, string>? environmentVariables = null,
        CancellationToken ct = default)
    {
        var isExe = ExecutableHelper.IsExecutable(toolPath);

        using var process = new Process();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        if (isExe)
        {
            // For single-file exe (Windows .exe or Unix extensionless), run directly
            process.StartInfo.FileName = toolPath;
        }
        else
        {
            // For DLL, use dotnet muxer
            var muxerPath = layout.GetMuxerPath();
            if (!IsValidMuxerPath(muxerPath))
            {
                throw new InvalidOperationException("Layout muxer not found");
            }
            process.StartInfo.FileName = muxerPath;
            process.StartInfo.ArgumentList.Add(toolPath);
        }

        // Set DOTNET_ROOT to use the layout's runtime
        var runtimePath = layout.GetComponentPath(LayoutComponent.Runtime);
        if (runtimePath is not null)
        {
            process.StartInfo.Environment["DOTNET_ROOT"] = runtimePath;
            process.StartInfo.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
        }

        // Add custom environment variables
        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                process.StartInfo.Environment[key] = value;
            }
        }

        if (workingDirectory is not null)
        {
            process.StartInfo.WorkingDirectory = workingDirectory;
        }

        // Add remaining arguments
        foreach (var arg in arguments)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        return (process.ExitCode, await outputTask, await errorTask);
    }

    /// <summary>
    /// Starts a managed process without waiting for it to exit.
    /// Returns the Process object for the caller to manage.
    /// </summary>
    public static Process StartManaged(
        LayoutConfiguration layout,
        string dllPath,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        IDictionary<string, string>? environmentVariables = null,
        bool redirectOutput = false)
    {
        var muxerPath = layout.GetMuxerPath();
        if (!IsValidMuxerPath(muxerPath))
        {
            throw new InvalidOperationException("Layout muxer not found");
        }

        var process = new Process();
        process.StartInfo.FileName = muxerPath;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        if (redirectOutput)
        {
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
        }

        // Set DOTNET_ROOT to use the layout's runtime
        var runtimePath = layout.GetComponentPath(LayoutComponent.Runtime);
        if (runtimePath is not null)
        {
            process.StartInfo.Environment["DOTNET_ROOT"] = runtimePath;
            process.StartInfo.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
        }

        // Add custom environment variables
        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                process.StartInfo.Environment[key] = value;
            }
        }

        if (workingDirectory is not null)
        {
            process.StartInfo.WorkingDirectory = workingDirectory;
        }

        // First argument is the DLL path
        process.StartInfo.ArgumentList.Add(dllPath);

        // Add remaining arguments
        foreach (var arg in arguments)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();
        return process;
    }
}
