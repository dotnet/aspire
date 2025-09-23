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

        var (success, highestVersion, minimumRequiredVersion) = await sdkInstaller.CheckAsync(cancellationToken);

        if (!success)
        {
            var detectedVersion = highestVersion ?? "(not found)";
            
            var sdkErrorMessage = string.Format(CultureInfo.InvariantCulture, 
                ErrorStrings.MinimumSdkVersionNotMet, 
                minimumRequiredVersion, 
                detectedVersion);
            interactionService.DisplayError(sdkErrorMessage);
            return false;
        }

        return true;
    }
}