// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Layout;
using Aspire.Cli.Projects;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Shared;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

/// <summary>
/// Extracts the embedded bundle payload from a self-extracting Aspire CLI binary.
/// </summary>
internal sealed class SetupCommand : BaseCommand
{
    private readonly ILayoutDiscovery _layoutDiscovery;
    private readonly ILogger<SetupCommand> _logger;

    private static readonly Option<string?> s_installPathOption = new("--install-path")
    {
        Description = "Directory to extract the bundle into. Defaults to the parent of the CLI binary's directory."
    };

    private static readonly Option<bool> s_forceOption = new("--force")
    {
        Description = "Force extraction even if the layout already exists."
    };

    public SetupCommand(
        ILayoutDiscovery layoutDiscovery,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        AspireCliTelemetry telemetry,
        ILogger<SetupCommand> logger)
        : base("setup", "Extract the embedded bundle to set up the Aspire CLI runtime.", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _layoutDiscovery = layoutDiscovery;
        _logger = logger;

        Options.Add(s_installPathOption);
        Options.Add(s_forceOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var installPath = parseResult.GetValue(s_installPathOption);
        var force = parseResult.GetValue(s_forceOption);

        var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
        {
            InteractionService.DisplayError("Could not determine the CLI executable path.");
            return ExitCodeConstants.FailedToBuildArtifacts;
        }

        var trailer = BundleTrailer.TryRead(processPath);
        if (trailer is null)
        {
            InteractionService.DisplayMessage(":information:", "This CLI binary does not contain an embedded bundle. No extraction needed.");
            return ExitCodeConstants.Success;
        }

        // Determine extraction directory
        if (string.IsNullOrEmpty(installPath))
        {
            var cliDir = Path.GetDirectoryName(processPath);
            installPath = !string.IsNullOrEmpty(cliDir) ? Path.GetDirectoryName(cliDir) ?? cliDir : cliDir;
        }

        if (string.IsNullOrEmpty(installPath))
        {
            InteractionService.DisplayError("Could not determine the installation path.");
            return ExitCodeConstants.FailedToBuildArtifacts;
        }

        // Check if layout already exists
        if (!force && _layoutDiscovery.DiscoverLayout() is not null)
        {
            InteractionService.DisplayMessage(":white_check_mark:", "Bundle is already extracted. Use --force to re-extract.");
            return ExitCodeConstants.Success;
        }

        // Extract with spinner
        var exitCode = await InteractionService.ShowStatusAsync(
            ":package:  Extracting Aspire bundle...",
            async () =>
            {
                await AppHostServerProjectFactory.ExtractPayloadAsync(processPath, trailer, installPath, cancellationToken);
                return ExitCodeConstants.Success;
            });

        if (_layoutDiscovery.DiscoverLayout() is not null)
        {
            InteractionService.DisplayMessage(":white_check_mark:", $"Bundle extracted to {installPath}");
        }
        else
        {
            InteractionService.DisplayError($"Bundle was extracted to {installPath} but layout validation failed.");
            return ExitCodeConstants.FailedToBuildArtifacts;
        }

        return exitCode;
    }
}
