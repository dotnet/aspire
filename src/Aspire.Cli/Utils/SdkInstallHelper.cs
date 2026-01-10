// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;

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
            // 1. The feature is enabled
            // 2. We support interactive input OR forceInstall is true (for testing)
            if (features.Enabled<DotNetSdkInstallationEnabledFeature>() &&
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
                        await interactionService.ShowStatusAsync(
                            string.Format(CultureInfo.InvariantCulture,
                                "Downloading and installing .NET SDK {0}... This may take a few minutes.",
                                minimumRequiredVersion),
                            async () =>
                            {
                                await sdkInstaller.InstallAsync(cancellationToken);
                                return 0; // Return dummy value for ShowStatusAsync
                            });

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