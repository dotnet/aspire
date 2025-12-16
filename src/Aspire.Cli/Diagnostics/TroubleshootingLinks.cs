// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Diagnostics;

/// <summary>
/// Provides troubleshooting links for different error scenarios.
/// </summary>
internal static class TroubleshootingLinks
{
    private const string BaseUrl = "https://aka.ms/aspire/troubleshoot";

    /// <summary>
    /// Gets the general troubleshooting link.
    /// </summary>
    public static string General => BaseUrl;

    /// <summary>
    /// Gets a troubleshooting link based on the exit code.
    /// </summary>
    /// <param name="exitCode">The exit code to get a link for.</param>
    /// <returns>A troubleshooting link.</returns>
    public static string GetLinkForExitCode(int exitCode)
    {
        return exitCode switch
        {
            ExitCodeConstants.FailedToFindProject => $"{BaseUrl}#project-not-found",
            ExitCodeConstants.FailedToBuildArtifacts => $"{BaseUrl}#build-failed",
            ExitCodeConstants.FailedToTrustCertificates => $"{BaseUrl}#certificate-trust",
            ExitCodeConstants.AppHostIncompatible => $"{BaseUrl}#version-mismatch",
            ExitCodeConstants.SdkNotInstalled => $"{BaseUrl}#sdk-missing",
            ExitCodeConstants.DashboardFailure => $"{BaseUrl}#dashboard-failed",
            _ => General
        };
    }
}
