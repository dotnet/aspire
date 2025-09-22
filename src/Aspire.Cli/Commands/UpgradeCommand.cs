// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Aspire.Shared;
using Microsoft.Extensions.Logging;
using Semver;

namespace Aspire.Cli.Commands;

internal sealed class UpgradeCommand : BaseCommand
{
    private readonly ILogger<UpgradeCommand> _logger;
    private readonly INuGetPackageCache _nuGetPackageCache;

    public UpgradeCommand(
        ILogger<UpgradeCommand> logger,
        INuGetPackageCache nuGetPackageCache,
        IInteractionService interactionService,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext)
        : base("upgrade", UpgradeCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(nuGetPackageCache);

        _logger = logger;
        _nuGetPackageCache = nuGetPackageCache;

        var prereleaseOption = new Option<bool>("--prerelease")
        {
            Description = UpgradeCommandStrings.PrereleaseOptionDescription,
            DefaultValueFactory = _ => false
        };
        Options.Add(prereleaseOption);

        var versionOption = new Option<string?>("--version");
        versionOption.Description = UpgradeCommandStrings.VersionOptionDescription;
        Options.Add(versionOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var includePrerelease = parseResult.GetValue<bool>("--prerelease");
        var targetVersion = parseResult.GetValue<string?>("--version");

        try
        {
            // Get current version
            var currentVersion = PackageUpdateHelpers.GetCurrentPackageVersion();
            if (currentVersion is null)
            {
                InteractionService.DisplayError(UpgradeCommandStrings.FailedToGetCurrentVersion);
                return ExitCodeConstants.FailedToUpgradeCliTool;
            }

            InteractionService.DisplayMessage(":information:", string.Format(CultureInfo.CurrentCulture, UpgradeCommandStrings.CurrentVersionMessage, currentVersion));

            // If a specific version is requested, use it directly
            if (!string.IsNullOrEmpty(targetVersion))
            {
                if (!SemVersion.TryParse(targetVersion, SemVersionStyles.Strict, out var parsedVersion))
                {
                    InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, UpgradeCommandStrings.InvalidVersionFormat, targetVersion));
                    return ExitCodeConstants.InvalidCommand;
                }

                return await PerformUpgradeAsync(targetVersion, cancellationToken);
            }

            // Check for available updates
            InteractionService.ShowStatus(UpgradeCommandStrings.CheckingForUpdatesMessage, async () =>
            {
                var availablePackages = await _nuGetPackageCache.GetCliPackagesAsync(
                    workingDirectory: ExecutionContext.WorkingDirectory,
                    prerelease: includePrerelease,
                    nugetConfigFile: null,
                    cancellationToken: cancellationToken);

                var newerVersion = PackageUpdateHelpers.GetNewerVersion(currentVersion, availablePackages);

                if (newerVersion is null)
                {
                    InteractionService.DisplaySuccess(UpgradeCommandStrings.AlreadyUpToDateMessage);
                    return;
                }

                InteractionService.DisplayMessage(":information:", string.Format(CultureInfo.CurrentCulture, UpgradeCommandStrings.NewVersionAvailableMessage, newerVersion));

                var shouldUpgrade = await InteractionService.ConfirmAsync(
                    string.Format(CultureInfo.CurrentCulture, UpgradeCommandStrings.ConfirmUpgradePrompt, newerVersion),
                    true,
                    cancellationToken);

                if (!shouldUpgrade)
                {
                    InteractionService.DisplayMessage(":information:", UpgradeCommandStrings.UpgradeCancelledMessage);
                    return;
                }

                await PerformUpgradeInternalAsync(newerVersion.ToString(), cancellationToken);
            });

            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upgrade CLI tool");
            InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, UpgradeCommandStrings.UpgradeFailedMessage, ex.Message));
            return ExitCodeConstants.FailedToUpgradeCliTool;
        }
    }

    private async Task<int> PerformUpgradeAsync(string version, CancellationToken cancellationToken)
    {
        try
        {
            await PerformUpgradeInternalAsync(version, cancellationToken);
            return ExitCodeConstants.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upgrade CLI tool to version {Version}", version);
            InteractionService.DisplayError(string.Format(CultureInfo.CurrentCulture, UpgradeCommandStrings.UpgradeFailedMessage, ex.Message));
            return ExitCodeConstants.FailedToUpgradeCliTool;
        }
    }

    private async Task PerformUpgradeInternalAsync(string version, CancellationToken cancellationToken)
    {
        await InteractionService.ShowStatusAsync(
            string.Format(CultureInfo.CurrentCulture, UpgradeCommandStrings.UpgradingToVersionMessage, version),
            async () =>
            {
                // Use dotnet tool update to upgrade the CLI
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"tool update -g Aspire.Cli --version {version}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                _logger.LogDebug("Executing: dotnet tool update -g Aspire.Cli --version {Version}", version);

                process.Start();

                var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    var errorMessage = !string.IsNullOrEmpty(stderr) ? stderr : stdout;
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, UpgradeCommandStrings.DotNetToolUpdateFailedMessage, process.ExitCode, errorMessage));
                }

                InteractionService.DisplaySuccess(string.Format(CultureInfo.CurrentCulture, UpgradeCommandStrings.UpgradeSuccessMessage, version));
                InteractionService.DisplayMessage(":information:", UpgradeCommandStrings.RestartShellMessage);

                return Task.CompletedTask;
            });
    }
}