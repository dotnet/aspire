// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.Execution;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="IBicepCompiler"/>.
/// </summary>
internal sealed class BicepCliCompiler : IBicepCompiler
{
    private readonly ILogger<BicepCliCompiler> _logger;
    private readonly IVirtualShell _shell;

    public BicepCliCompiler(ILogger<BicepCliCompiler> logger, IVirtualShell shell)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
    }

    public async Task<string> CompileBicepToArmAsync(string bicepFilePath, CancellationToken cancellationToken = default)
    {
        // Try bicep command first for better performance
        var bicepPath = PathLookupHelper.FindFullPathFromPath("bicep");
        string command;
        string[] args;

        if (bicepPath is not null)
        {
            command = bicepPath;
            args = ["build", bicepFilePath, "--stdout"];
        }
        else
        {
            // Fall back to az bicep if bicep command is not available
            var azPath = PathLookupHelper.FindFullPathFromPath("az");
            if (azPath is null)
            {
                throw new AzureCliNotOnPathException();
            }
            command = azPath;
            args = ["bicep", "build", "--file", bicepFilePath, "--stdout"];
        }

        _logger.LogDebug("Running {CommandPath} with arguments: {Arguments}", command, string.Join(" ", args));

        var result = await _shell.Command(command, args).RunAsync(ct: cancellationToken).ConfigureAwait(false);

        result.LogOutput(_logger, command);

        if (result.ExitCode != 0)
        {
            _logger.LogError("Bicep compilation for {BicepFilePath} failed with exit code {ExitCode}.", bicepFilePath, result.ExitCode);
            throw new InvalidOperationException($"Failed to compile bicep file: {bicepFilePath}");
        }

        _logger.LogDebug("Bicep compilation for {BicepFilePath} succeeded.", bicepFilePath);

        return result.Stdout ?? string.Empty;
    }
}
