// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Aspire.Cli.Utils;

/// <summary>
/// Helper class for managing SDK installation UX and user interaction.
/// </summary>
internal static class SdkInstallHelper
{
    /// <summary>
    /// Ensures that the .NET SDK is installed and available, displaying an error message if it's not.
    /// If the SDK is missing, prompts the user to install it automatically (if in interactive mode and feature is enabled).
    /// </summary>
    /// <param name="sdkInstaller">The SDK installer service.</param>
    /// <param name="interactionService">The interaction service for user communication.</param>
    /// <param name="features">The features service for checking if SDK installation is enabled.</param>
    /// <param name="hostEnvironment">The CLI host environment for detecting interactive capabilities.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the SDK is available, false if it's missing.</returns>
    public static async Task<bool> EnsureSdkInstalledAsync(
        IDotNetSdkInstaller sdkInstaller,
        IInteractionService interactionService,
        IFeatures features,
        ICliHostEnvironment? hostEnvironment = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sdkInstaller);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(features);

        var (success, highestVersion, minimumRequiredVersion, forceInstall) = await sdkInstaller.CheckAsync(cancellationToken);

        if (!success || forceInstall)
        {
            var detectedVersion = highestVersion ?? "(not found)";
            
            // Only display error if SDK is actually missing
            if (!success)
            {
                var sdkErrorMessage = string.Format(CultureInfo.InvariantCulture, 
                    ErrorStrings.MinimumSdkVersionNotMet, 
                    minimumRequiredVersion, 
                    detectedVersion);
                interactionService.DisplayError(sdkErrorMessage);
            }

            // Only offer to install if:
            // 1. The feature is enabled (default: false)
            // 2. We support interactive input OR forceInstall is true (for testing)
            if (features.IsFeatureEnabled(KnownFeatures.DotNetSdkInstallationEnabled, defaultValue: false) &&
                (hostEnvironment?.SupportsInteractiveInput == true || forceInstall))
            {
                bool shouldInstall;
                
                if (forceInstall)
                {
                    // When alwaysInstallSdk is true, skip the prompt and install directly
                    shouldInstall = true;
                    interactionService.DisplayMessage("information", 
                        "alwaysInstallSdk is enabled - forcing SDK installation for testing purposes.");
                }
                else
                {
                    // Offer to install the SDK automatically
                    shouldInstall = await interactionService.ConfirmAsync(
                        string.Format(CultureInfo.InvariantCulture,
                            "Would you like to install .NET SDK {0} automatically?",
                            minimumRequiredVersion),
                        defaultValue: true,
                        cancellationToken: cancellationToken);
                }

                if (shouldInstall)
                {
                    try
                    {
                        // Check if we're in an environment that supports progress display
                        var supportsProgress = hostEnvironment?.SupportsInteractiveOutput == true && 
                                             hostEnvironment?.SupportsAnsi == true;
                        
                        if (supportsProgress)
                        {
                            await AnsiConsole.Progress()
                                .AutoClear(false)
                                .HideCompleted(false)
                                .Columns(
                                    new TaskDescriptionColumn(),
                                    new ProgressBarColumn(),
                                    new PercentageColumn(),
                                    new DownloadedColumn(),
                                    new TransferSpeedColumn())
                                .StartAsync(async ctx =>
                                {
                                    var downloadTask = ctx.AddTask($"Downloading .NET SDK {minimumRequiredVersion}", maxValue: 100);
                                    
                                    await sdkInstaller.InstallAsync(
                                        progressCallback: (bytesDownloaded, totalBytes) =>
                                        {
                                            if (totalBytes > 0 && downloadTask.MaxValue != totalBytes)
                                            {
                                                downloadTask.MaxValue = totalBytes;
                                            }
                                            downloadTask.Value = bytesDownloaded;
                                        },
                                        cancellationToken: cancellationToken);
                                    
                                    downloadTask.StopTask();
                                });
                        }
                        else
                        {
                            // Fallback for non-interactive environments
                            await interactionService.ShowStatusAsync(
                                string.Format(CultureInfo.InvariantCulture,
                                    "Downloading and installing .NET SDK {0}... This may take a few minutes.",
                                    minimumRequiredVersion),
                                async () =>
                                {
                                    await sdkInstaller.InstallAsync(cancellationToken: cancellationToken);
                                    return 0;
                                });
                        }

                        interactionService.DisplaySuccess(
                            string.Format(CultureInfo.InvariantCulture,
                                ".NET SDK {0} has been installed successfully.",
                                minimumRequiredVersion));

                        return true;
                    }
                    catch (Exception ex)
                    {
                        interactionService.DisplayError(
                            string.Format(CultureInfo.InvariantCulture,
                                "Failed to install .NET SDK: {0}",
                                ex.Message));
                        return false;
                    }
                }
            }

            // If we didn't install and SDK check failed, return false
            return success;
        }

        return true;
    }
}

/// <summary>
/// A progress column that displays the number of bytes downloaded.
/// </summary>
file sealed class DownloadedColumn : ProgressColumn
{
    private static readonly string[] s_sizeUnits = ["B", "KB", "MB", "GB"];

    /// <inheritdoc />
    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        var downloaded = FormatBytes((long)task.Value);
        var total = task.MaxValue > 0 ? FormatBytes((long)task.MaxValue) : "?";
        return new Markup($"[cyan]{downloaded}[/]/[dim]{total}[/]");
    }

    private static string FormatBytes(long bytes)
    {
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < s_sizeUnits.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {s_sizeUnits[order]}";
    }
}

/// <summary>
/// A progress column that displays the transfer speed.
/// </summary>
file sealed class TransferSpeedColumn : ProgressColumn
{
    private const double SpeedUpdateIntervalSeconds = 0.5;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<int, (long BytesRead, DateTime LastUpdate)> _taskData = new();

    /// <inheritdoc />
    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        var now = DateTime.UtcNow;
        var currentBytes = (long)task.Value;

        if (!_taskData.TryGetValue(task.Id, out var data))
        {
            _taskData[task.Id] = (currentBytes, now);
            return new Markup("[dim]-- MB/s[/]");
        }

        var elapsed = (now - data.LastUpdate).TotalSeconds;
        if (elapsed < SpeedUpdateIntervalSeconds)
        {
            return new Markup("[dim]-- MB/s[/]");
        }

        var bytesPerSecond = elapsed > 0 ? (currentBytes - data.BytesRead) / elapsed : 0;
        _taskData[task.Id] = (currentBytes, now);

        var speed = FormatSpeed(bytesPerSecond);
        return new Markup($"[yellow]{speed}[/]");
    }

    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond < 1024)
        {
            return $"{bytesPerSecond:0.##} B/s";
        }
        if (bytesPerSecond < 1024 * 1024)
        {
            return $"{bytesPerSecond / 1024:0.##} KB/s";
        }
        return $"{bytesPerSecond / (1024 * 1024):0.##} MB/s";
    }
}