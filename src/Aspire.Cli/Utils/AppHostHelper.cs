// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Semver;
using System.Diagnostics;
using System.Globalization;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Utils;

internal static class AppHostHelper
{
    internal static async Task<(bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingVersion)> CheckAppHostCompatibilityAsync(IDotNetCliRunner runner, IInteractionService interactionService, FileInfo projectFile, AspireCliTelemetry telemetry, DirectoryInfo workingDirectory, CancellationToken cancellationToken)
    {
        var appHostInformation = await GetAppHostInformationAsync(runner, interactionService, projectFile, telemetry, workingDirectory, cancellationToken);

        if (appHostInformation.ExitCode != 0)
        {
            interactionService.DisplayError(ErrorStrings.ProjectCouldNotBeAnalyzed);
            return (false, false, null);
        }

        if (!appHostInformation.IsAspireHost)
        {
            interactionService.DisplayError(ErrorStrings.ProjectIsNotAppHost);
            return (false, false, null);
        }

        if (!SemVersion.TryParse(appHostInformation.AspireHostingVersion, out var aspireVersion))
        {
            interactionService.DisplayError(ErrorStrings.CouldNotParseAspireSDKVersion);
            return (false, false, null);
        }

        var minimumVersion = SemVersion.Parse("9.2.0");
        // Compare only major.minor.patch, ignoring prerelease tags
        // This ensures that preview versions (e.g., 13.0.0-preview.1) are accepted
        // as long as their major.minor.patch is >= the minimum version
        if (CompareMajorMinorPatch(aspireVersion, minimumVersion) < 0)
        {
            interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ErrorStrings.AspireSDKVersionNotSupported, appHostInformation.AspireHostingVersion));
            return (false, false, appHostInformation.AspireHostingVersion);
        }
        else
        {
            // NOTE: When we go to support < 9.2.0 app hosts this is where we'll make
            //       a determination as to whether the apphsot supports backchannel or not.
            return (true, true, appHostInformation.AspireHostingVersion);
        }
    }

    internal static async Task<(int ExitCode, bool IsAspireHost, string? AspireHostingVersion)> GetAppHostInformationAsync(IDotNetCliRunner runner, IInteractionService interactionService, FileInfo projectFile, AspireCliTelemetry telemetry, DirectoryInfo workingDirectory, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity(nameof(GetAppHostInformationAsync), ActivityKind.Client);

        var relativePath = Path.GetRelativePath(workingDirectory.FullName, projectFile.FullName);
        var appHostInformationResult = await interactionService.ShowStatusAsync(
            $":microscope: {InteractionServiceStrings.CheckingProjectType}: {relativePath}",
            () => runner.GetAppHostInformationAsync(
                projectFile,
                new DotNetCliRunnerInvocationOptions(),
                cancellationToken));

        return appHostInformationResult;
    }

    internal static async Task<int> BuildAppHostAsync(IDotNetCliRunner runner, IInteractionService interactionService, FileInfo projectFile, DotNetCliRunnerInvocationOptions options, DirectoryInfo workingDirectory, CancellationToken cancellationToken)
    {
        var relativePath = Path.GetRelativePath(workingDirectory.FullName, projectFile.FullName);
        return await interactionService.ShowStatusAsync(
            $":hammer_and_wrench:  {InteractionServiceStrings.BuildingAppHost} {relativePath}",
            () => runner.BuildAsync(
                projectFile,
                options,
                cancellationToken));
    }

    /// <summary>
    /// Compares two semantic versions by only their major, minor, and patch components,
    /// ignoring prerelease identifiers and build metadata.
    /// </summary>
    /// <param name="version">The version to compare.</param>
    /// <param name="other">The version to compare against.</param>
    /// <returns>
    /// A negative value if <paramref name="version"/> is less than <paramref name="other"/>,
    /// zero if they are equal, or a positive value if <paramref name="version"/> is greater.
    /// </returns>
    private static int CompareMajorMinorPatch(SemVersion version, SemVersion other)
    {
        if (version.Major != other.Major)
        {
            return version.Major.CompareTo(other.Major);
        }

        if (version.Minor != other.Minor)
        {
            return version.Minor.CompareTo(other.Minor);
        }

        return version.Patch.CompareTo(other.Patch);
    }
}
