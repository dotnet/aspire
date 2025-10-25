// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.DotNet;

/// <summary>
/// Service responsible for checking and installing the .NET SDK.
/// </summary>
internal interface IDotNetSdkInstaller
{
    /// <summary>
    /// Checks if the .NET SDK is available on the system PATH and determines if it should be installed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="CheckInstallResult"/> containing the check results and installation determination.</returns>
    Task<CheckInstallResult> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs the .NET SDK. This method is reserved for future extensibility.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InstallAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective minimum SDK version based on configuration and feature flags.
    /// </summary>
    /// <returns>The minimum SDK version string.</returns>
    string GetEffectiveMinimumSdkVersion();
}