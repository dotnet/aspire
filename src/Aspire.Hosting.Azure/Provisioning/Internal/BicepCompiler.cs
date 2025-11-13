// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using Aspire.Hosting.Dcp.Process;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="IBicepCompiler"/>.
/// </summary>
internal sealed class BicepCliCompiler : IBicepCompiler
{
    private readonly ILogger<BicepCliCompiler> _logger;

    public BicepCliCompiler(ILogger<BicepCliCompiler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> CompileBicepToArmAsync(string bicepFilePath, CancellationToken cancellationToken = default)
    {
        // Try bicep command first for better performance
        var bicepPath = FindFullPathFromPath("bicep");
        string commandPath;
        string arguments;

        if (bicepPath is not null)
        {
            commandPath = bicepPath;
            arguments = $"build \"{bicepFilePath}\" --stdout";
        }
        else
        {
            // Fall back to az bicep if bicep command is not available
            var azPath = FindFullPathFromPath("az");
            if (azPath is null)
            {
                throw new AzureCliNotOnPathException();
            }
            commandPath = azPath;
            arguments = $"bicep build --file \"{bicepFilePath}\" --stdout";
        }

        var armTemplateContents = new StringBuilder();
        var templateSpec = new ProcessSpec(commandPath)
        {
            Arguments = arguments,
            OnOutputData = data =>
            {
                _logger.LogDebug("{CommandPath} (stdout): {Output}", commandPath, data);
                armTemplateContents.AppendLine(data);
            },
            OnErrorData = data =>
            {
                _logger.LogDebug("{CommandPath} (stderr): {Error}", commandPath, data);
            },
        };

        _logger.LogDebug("Running {CommandPath} with arguments: {Arguments}", commandPath, arguments);

        var exitCode = await ExecuteCommand(templateSpec).ConfigureAwait(false);

        if (exitCode != 0)
        {
            _logger.LogError("Bicep compilation for {BicepFilePath} failed with exit code {ExitCode}.", bicepFilePath, exitCode);
            throw new InvalidOperationException($"Failed to compile bicep file: {bicepFilePath}");
        }

        _logger.LogInformation("Bicep compilation for {BicepFilePath} succeeded.", bicepFilePath);

        return armTemplateContents.ToString();
    }

    private static async Task<int> ExecuteCommand(ProcessSpec processSpec)
    {
        var sw = Stopwatch.StartNew();
        var (task, disposable) = ProcessUtil.Run(processSpec);

        try
        {
            var result = await task.ConfigureAwait(false);
            sw.Stop();

            return result.ExitCode;
        }
        finally
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal static string? FindFullPathFromPath(string command)
    {
        return FindFullPathFromPath(command, Environment.GetEnvironmentVariable("PATH"), File.Exists);
    }

    private static string? FindFullPathFromPath(string command, string? pathVariable, Func<string, bool> fileExists)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(command));

        if (OperatingSystem.IsWindows())
        {
            command += ".cmd";
        }

        foreach (var directory in (pathVariable ?? string.Empty).Split(Path.PathSeparator))
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
