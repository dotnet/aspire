// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Utils;

/// <summary>
/// Result of an SDK availability check.
/// </summary>
internal enum SdkInstallResult
{
    /// <summary>
    /// A valid SDK was already installed.
    /// </summary>
    AlreadyInstalled,

    /// <summary>
    /// The SDK is missing or does not meet the minimum required version.
    /// </summary>
    NotInstalled
}
