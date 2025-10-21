// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.DotNet;

/// <summary>
/// Service responsible for checking and installing the .NET SDK.
/// </summary>
internal interface IDotNetSdkInstaller
{
    /// <summary>
    /// Checks if the .NET SDK is available on the system PATH.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing: success flag, highest detected version, minimum required version, and whether to force installation.</returns>
    Task<(bool Success, string? HighestVersion, string MinimumRequiredVersion, bool ForceInstall)> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs the .NET SDK. This method is reserved for future extensibility.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InstallAsync(CancellationToken cancellationToken = default);
}