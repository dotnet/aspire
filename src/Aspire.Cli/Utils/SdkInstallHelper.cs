// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
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
    /// If the SDK is missing and installation is determined automatically based on feature flags, installs it.
    /// </summary>
    /// <param name="sdkInstaller">The SDK installer service.</param>
    /// <param name="interactionService">The interaction service for user communication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the SDK is available, false if it's missing.</returns>
    public static async Task<bool> EnsureSdkInstalledAsync(
        IDotNetSdkInstaller sdkInstaller,
        IInteractionService interactionService,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sdkInstaller);
        ArgumentNullException.ThrowIfNull(interactionService);

        var result = await sdkInstaller.CheckAsync(cancellationToken);

        if (!result.Success || result.ForceInstall)
        {
            var detectedVersion = result.HighestVersion ?? "(not found)";
            
            // Only display error if SDK is actually missing
            if (!result.Success)
            {
                var sdkErrorMessage = string.Format(CultureInfo.InvariantCulture, 
                    ErrorStrings.MinimumSdkVersionNotMet, 
                    result.MinimumRequiredVersion, 
                    detectedVersion);
                interactionService.DisplayError(sdkErrorMessage);
            }

            // Install automatically if determined by CheckAsync
            if (result.ShouldInstall)
            {
                if (result.ForceInstall)
                {
                    interactionService.DisplayMessage("information", 
                        "alwaysInstallSdk is enabled - forcing SDK installation for testing purposes.");
                }

                try
                {
                    await interactionService.ShowStatusAsync(
                        string.Format(CultureInfo.InvariantCulture,
                            "Downloading and installing .NET SDK {0}... This may take a few minutes.",
                            result.MinimumRequiredVersion),
                        async () =>
                        {
                            await sdkInstaller.InstallAsync(cancellationToken);
                            return 0; // Return dummy value for ShowStatusAsync
                        });

                    interactionService.DisplaySuccess(
                        string.Format(CultureInfo.InvariantCulture,
                            ".NET SDK {0} has been installed successfully.",
                            result.MinimumRequiredVersion));

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

            // If we didn't install and SDK check failed, return false
            return result.Success;
        }

        return true;
    }
}