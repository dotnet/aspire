// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.DotNet;

/// <summary>
/// Represents the result of checking whether the .NET SDK needs to be installed.
/// </summary>
internal sealed class CheckInstallResult
{
    /// <summary>
    /// Gets a value indicating whether the SDK check was successful (SDK meets minimum requirements).
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the highest SDK version detected on the system, or null if no SDK was found.
    /// </summary>
    public string? HighestVersion { get; init; }

    /// <summary>
    /// Gets the minimum required SDK version for Aspire.
    /// </summary>
    public string MinimumRequiredVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether installation should be forced regardless of SDK availability.
    /// </summary>
    public bool ForceInstall { get; init; }

    /// <summary>
    /// Gets a value indicating whether the SDK should be installed based on feature flags and environment capabilities.
    /// </summary>
    public bool ShouldInstall { get; init; }
}