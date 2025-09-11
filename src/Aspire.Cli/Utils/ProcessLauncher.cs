// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.DotNet;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils;

/// <summary>
/// Default implementation of <see cref="IProcessLauncher"/> that uses the configured .NET runtime.
/// </summary>
internal class ProcessLauncher(
    ILogger<ProcessLauncher> logger,
    IDotNetRuntimeSelector runtimeSelector) : IProcessLauncher
{
    /// <inheritdoc />
    public async Task<int> LaunchDotNetAsync(
        string arguments,
        string? workingDirectory = null,
        IDictionary<string, string>? environmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        return await LaunchAsync(
            await runtimeSelector.GetDotNetExecutablePathAsync(cancellationToken),
            arguments,
            workingDirectory,
            environmentVariables,
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<int> LaunchAsync(
        string executablePath,
        string? arguments = null,
        string? workingDirectory = null,
        IDictionary<string, string>? environmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = arguments ?? string.Empty,
            UseShellExecute = false,
            CreateNoWindow = false
        };

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        // Apply environment variables from runtime selector
        var runtimeEnvVars = await runtimeSelector.GetEnvironmentVariablesAsync(cancellationToken);
        foreach (var kvp in runtimeEnvVars)
        {
            startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
        }

        // Apply additional environment variables
        if (environmentVariables != null)
        {
            foreach (var kvp in environmentVariables)
            {
                startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
            }
        }

        logger.LogDebug("Launching process: {Executable} {Arguments}", executablePath, arguments);

        using var process = new Process { StartInfo = startInfo };
        
        process.Start();
        await process.WaitForExitAsync(cancellationToken);

        logger.LogDebug("Process exited with code: {ExitCode}", process.ExitCode);
        
        return process.ExitCode;
    }
}