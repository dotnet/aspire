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
    /// </summary>
    /// <param name="sdkInstaller">The SDK installer service.</param>
    /// <param name="interactionService">The interaction service for user communication.</param>
    /// <param name="features">The features service to check for enabled flags.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the SDK is available, false if it's missing.</returns>
    public static async Task<bool> EnsureSdkInstalledAsync(
        IDotNetSdkInstaller sdkInstaller,
        IInteractionService interactionService,
        IFeatures features,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sdkInstaller);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(features);

        var isSdkAvailable = await sdkInstaller.CheckAsync(cancellationToken);

        if (!isSdkAvailable)
        {
            var requiredVersion = sdkInstaller.GetEffectiveMinimumSdkVersion();
            var detectedVersion = await sdkInstaller.GetInstalledSdkVersionAsync(cancellationToken) ?? "(not found)";
            
            var flagSuffix = features.IsFeatureEnabled(KnownFeatures.SingleFileAppHostEnabled, false)
                ? " with 'singlefileAppHostEnabled' (disable the feature flag or install a .NET 10 SDK)"
                : "";

            var sdkErrorMessage = string.Format(CultureInfo.InvariantCulture, 
                ErrorStrings.MinimumSdkVersionNotMet, 
                flagSuffix, 
                requiredVersion, 
                detectedVersion);
            interactionService.DisplayError(sdkErrorMessage);
            return false;
        }

        return true;
    }
}