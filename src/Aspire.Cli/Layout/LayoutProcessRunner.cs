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
/// Utilities for running processes using the layout's .NET runtime.
/// Supports both native executables and framework-dependent DLLs.
/// </summary>
internal static class LayoutProcessRunner
{
    /// <summary>
    /// Determines if a path refers to a DLL that needs dotnet to run.
    /// </summary>
    private static bool IsDll(string path) => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Runs a tool and captures output. Automatically detects if the tool
    /// is a DLL (needs muxer) or native executable (runs directly).
    /// </summary>
    public static async Task<(int ExitCode, string Output, string Error)> RunAsync(
        LayoutConfiguration layout,
        string toolPath,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        IDictionary<string, string>? environmentVariables = null,
        CancellationToken ct = default)
    {
        using var process = CreateProcess(layout, toolPath, arguments, workingDirectory, environmentVariables, redirectOutput: true);

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        return (process.ExitCode, await outputTask, await errorTask);
    }

    /// <summary>
    /// Starts a process without waiting for it to exit.
    /// Returns the Process object for the caller to manage.
    /// </summary>
    public static Process Start(
        LayoutConfiguration layout,
        string toolPath,
        IEnumerable<string> arguments,
        string? workingDirectory = null,
        IDictionary<string, string>? environmentVariables = null,
        bool redirectOutput = false)
    {
        var process = CreateProcess(layout, toolPath, arguments, workingDirectory, environmentVariables, redirectOutput);
        process.Start();
        return process;
    }

    /// <summary>
    /// Creates a configured Process for running a bundle tool.
    /// For DLLs, uses the layout's muxer (dotnet). For executables, runs directly.
    /// </summary>
    private static Process CreateProcess(
        LayoutConfiguration layout,
        string toolPath,
        IEnumerable<string> arguments,
        string? workingDirectory,
        IDictionary<string, string>? environmentVariables,
        bool redirectOutput)
    {
        var isDll = IsDll(toolPath);
        var process = new Process();

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        if (isDll)
        {
            // DLLs need the muxer to run
            var muxerPath = layout.GetMuxerPath()
                ?? throw new InvalidOperationException("Layout muxer not found. Cannot run framework-dependent tool.");
            process.StartInfo.FileName = muxerPath;
            process.StartInfo.ArgumentList.Add(toolPath);
        }
        else
        {
            // Native executables run directly
            process.StartInfo.FileName = toolPath;
        }

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

        // Add arguments
        foreach (var arg in arguments)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        return process;
    }
}
