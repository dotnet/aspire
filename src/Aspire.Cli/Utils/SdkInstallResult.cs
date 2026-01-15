// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Utils;

/// <summary>
/// Result of an SDK installation attempt.
/// </summary>
internal enum SdkInstallResult
{
    /// <summary>
    /// A valid SDK was already installed.
    /// </summary>
    AlreadyInstalled,

    /// <summary>
    /// The SDK was successfully installed during this operation.
    /// </summary>
    Installed,

    /// <summary>
    /// The SDK installation feature is not enabled.
    /// </summary>
    FeatureNotEnabled,

    /// <summary>
    /// The CLI is not running in an interactive environment.
    /// </summary>
    NotInteractive,

    /// <summary>
    /// The user declined the installation prompt.
    /// </summary>
    UserDeclined,

    /// <summary>
    /// An error occurred during installation.
    /// </summary>
    InstallError
}
